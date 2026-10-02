// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.assertNotNull;
import static org.junit.jupiter.api.Assertions.assertNull;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.Closeable;
import java.io.EOFException;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.InetAddress;
import java.net.ServerSocket;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.Base64;
import java.util.HashMap;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicReference;

import org.junit.jupiter.api.Test;

import io.reactivex.rxjava3.core.Completable;
import okhttp3.OkHttpClient;

class OkHttpWebSocketWrapperTest {

    // OkHttp waits RealWebSocket.CANCEL_AFTER_CLOSE_MILLIS, 60 seconds, before it cancels a close the
    // peer never answered. Anything below that proves the wrapper bounded the wait itself.
    private static final long OKHTTP_CANCEL_AFTER_CLOSE_MILLIS = 60 * 1000;

    // Mirrors OkHttpWebSocketWrapper.CLOSE_TIMEOUT_MILLIS. A stop that finishes below this did not come
    // back on the ungraceful close timer.
    private static final long CLOSE_TIMEOUT_MILLIS = 5 * 1000;

    @Test
    public void stopCompletesWhenThePeerNeverAnswersTheCloseFrame() throws Exception {
        try (LoopbackWebSocketServer server = new LoopbackWebSocketServer(false)) {
            OkHttpClient client = new OkHttpClient();
            OkHttpWebSocketWrapper wrapper = new OkHttpWebSocketWrapper(server.getWebSocketUrl(), new HashMap<>(), client);
            wrapper.setOnReceive((message) -> { });
            wrapper.setOnClose((code, reason) -> { });

            assertTrue(wrapper.start().blockingAwait(30, TimeUnit.SECONDS), "The WebSocket never connected.");

            long startedAt = System.nanoTime();
            Completable stop = wrapper.stop();

            assertTrue(server.awaitCloseFrame(30, TimeUnit.SECONDS),
                    "The client never sent a close frame, so this run did not exercise an unanswered close.");

            boolean stopped = stop.onErrorComplete().blockingAwait(30, TimeUnit.SECONDS);
            long elapsedMillis = (System.nanoTime() - startedAt) / 1_000_000;

            assertTrue(stopped, "stop() did not complete while the peer left the close frame unanswered.");
            assertTrue(elapsedMillis < OKHTTP_CANCEL_AFTER_CLOSE_MILLIS,
                    "stop() took " + elapsedMillis + "ms, so it waited on OkHttp's cancel instead of bounding the close.");
        }
    }

    @Test
    public void stopWaitsForTheCloseHandshakeWhenThePeerAnswersTheCloseFrame() throws Exception {
        try (LoopbackWebSocketServer server = new LoopbackWebSocketServer(true);
                DefaultHttpClient httpClient = new DefaultHttpClient(new OkHttpClient(), null)) {
            WebSocketTransport transport = new WebSocketTransport(new HashMap<>(), httpClient);
            AtomicBoolean closeCallbackRan = new AtomicBoolean(false);
            AtomicReference<String> closeReason = new AtomicReference<>("the close callback never ran");
            transport.setOnReceive((message) -> { });
            transport.setOnClose((reason) -> {
                closeReason.set(reason);
                closeCallbackRan.set(true);
            });

            assertTrue(transport.start(server.getUrl()).blockingAwait(30, TimeUnit.SECONDS),
                    "The WebSocket transport never connected.");

            long startedAt = System.nanoTime();
            boolean stopped = transport.stop().onErrorComplete().blockingAwait(30, TimeUnit.SECONDS);
            long elapsedMillis = (System.nanoTime() - startedAt) / 1_000_000;

            assertTrue(stopped, "stop() did not complete against a peer that answered the close frame.");

            // The peer delays its answer, so a stop that came back before it cannot have waited for the
            // close handshake. onClosing also invokes the close callback before completing closeSubject,
            // so a stop that did wait has already run it.
            assertTrue(elapsedMillis >= LoopbackWebSocketServer.CLOSE_ANSWER_DELAY_MILLIS,
                    "stop() completed in " + elapsedMillis + "ms, before the peer answered the close frame, so it did not wait for the close handshake.");
            assertTrue(closeCallbackRan.get(),
                    "stop() completed before any close callback ran, so it returned on the close timer rather than on the close handshake.");
            assertNull(closeReason.get(),
                    "A clean 1000 close was reported to the transport as an error close, reason: " + closeReason.get());
            assertTrue(elapsedMillis < CLOSE_TIMEOUT_MILLIS,
                    "stop() took " + elapsedMillis + "ms, so it waited out the close timeout instead of the peer's close frame.");
        }
    }

    @Test
    public void stopReportsAnErrorCloseWhenTheCloseTimeoutCancelsTheSocket() throws Exception {
        try (LoopbackWebSocketServer server = new LoopbackWebSocketServer(false);
                DefaultHttpClient httpClient = new DefaultHttpClient(new OkHttpClient(), null)) {
            WebSocketTransport transport = new WebSocketTransport(new HashMap<>(), httpClient);
            CountDownLatch closeCallbackRan = new CountDownLatch(1);
            AtomicReference<String> closeReason = new AtomicReference<>();
            transport.setOnReceive((message) -> { });
            transport.setOnClose((reason) -> {
                closeReason.set(reason);
                closeCallbackRan.countDown();
            });

            assertTrue(transport.start(server.getUrl()).blockingAwait(30, TimeUnit.SECONDS),
                    "The WebSocket transport never connected.");

            assertTrue(transport.stop().onErrorComplete().blockingAwait(30, TimeUnit.SECONDS),
                    "stop() did not complete while the peer left the close frame unanswered.");

            // Cancelling reports the socket as failed rather than cleanly closed, so unlike the answered
            // close above the transport sees a reason instead of null. That is the same tradeoff the .NET
            // client makes when its CloseTimeout expires.
            assertTrue(closeCallbackRan.await(30, TimeUnit.SECONDS), "The close callback never ran.");
            assertNotNull(closeReason.get(),
                    "A close the peer never answered was reported as a clean close, which hides the ungraceful shutdown.");
        }
    }

    // A peer that completes the WebSocket handshake and then either answers the client's close frame or
    // deliberately leaves it unanswered. TestHttpClient runs its handlers inline on the caller's thread,
    // so only a real socket can hold a close handshake open like this.
    private static final class LoopbackWebSocketServer implements Closeable {
        private static final String WEBSOCKET_GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private static final String KEY_HEADER = "Sec-WebSocket-Key:";
        private static final int OPCODE_CLOSE = 0x8;

        // Answer the close frame late enough that a stop which never waited for the close handshake is
        // distinguishable from one that did, but well inside the wrapper's close timeout. Without this a
        // loopback peer answers so fast that both look identical from the test thread.
        static final long CLOSE_ANSWER_DELAY_MILLIS = 750;

        private final ServerSocket serverSocket;
        private final boolean answerClose;
        private final CountDownLatch closeFrameReceived = new CountDownLatch(1);
        private volatile Socket connection;

        LoopbackWebSocketServer(boolean answerClose) throws IOException {
            this.answerClose = answerClose;
            this.serverSocket = new ServerSocket(0, 1, InetAddress.getLoopbackAddress());
            Thread thread = new Thread(this::run, "loopback-websocket-server");
            thread.setDaemon(true);
            thread.start();
        }

        String getUrl() {
            return "http://" + serverSocket.getInetAddress().getHostAddress() + ":" + serverSocket.getLocalPort();
        }

        String getWebSocketUrl() {
            return "ws://" + serverSocket.getInetAddress().getHostAddress() + ":" + serverSocket.getLocalPort();
        }

        boolean awaitCloseFrame(long timeout, TimeUnit unit) throws InterruptedException {
            return closeFrameReceived.await(timeout, unit);
        }

        private void run() {
            try (Socket socket = serverSocket.accept()) {
                connection = socket;
                InputStream input = socket.getInputStream();
                OutputStream output = socket.getOutputStream();

                String accept = acceptFor(readWebSocketKey(input));
                output.write(("HTTP/1.1 101 Switching Protocols\r\n"
                        + "Upgrade: websocket\r\n"
                        + "Connection: Upgrade\r\n"
                        + "Sec-WebSocket-Accept: " + accept + "\r\n\r\n").getBytes(StandardCharsets.UTF_8));
                output.flush();

                // Nothing is ever sent over this connection, so the first frame the client writes is its
                // close frame.
                int firstByte = input.read();
                if (firstByte != -1 && (firstByte & 0x0F) == OPCODE_CLOSE) {
                    consumeFrameBody(input);
                    closeFrameReceived.countDown();

                    if (answerClose) {
                        Thread.sleep(CLOSE_ANSWER_DELAY_MILLIS);
                        // An unmasked close frame carrying status code 1000.
                        output.write(new byte[] { (byte) 0x88, 0x02, 0x03, (byte) 0xE8 });
                        output.flush();
                    }
                }

                while (input.read() != -1) {
                    // Hold the connection open until the client gives up or finishes closing.
                }
            } catch (Exception ex) {
                // The test closes the sockets out from under this thread when it finishes.
            }
        }

        // Reads the rest of a client frame, which is always masked and, for a close, always short.
        private static void consumeFrameBody(InputStream input) throws IOException {
            int second = readByte(input);
            int length = second & 0x7F;
            if ((second & 0x80) != 0) {
                skip(input, 4);
            }

            skip(input, length);
        }

        private static void skip(InputStream input, int count) throws IOException {
            for (int i = 0; i < count; i++) {
                readByte(input);
            }
        }

        private static int readByte(InputStream input) throws IOException {
            int value = input.read();
            if (value == -1) {
                throw new EOFException("The client closed the socket mid frame.");
            }

            return value;
        }

        private static String readWebSocketKey(InputStream input) throws IOException {
            StringBuilder request = new StringBuilder();
            int b;
            while ((b = input.read()) != -1) {
                request.append((char) b);
                if (request.length() >= 4 && request.lastIndexOf("\r\n\r\n") == request.length() - 4) {
                    break;
                }
            }

            for (String line : request.toString().split("\r\n")) {
                if (line.regionMatches(true, 0, KEY_HEADER, 0, KEY_HEADER.length())) {
                    return line.substring(KEY_HEADER.length()).trim();
                }
            }

            throw new IOException("The upgrade request did not contain a " + KEY_HEADER + " header.");
        }

        private static String acceptFor(String key) throws Exception {
            MessageDigest sha1 = MessageDigest.getInstance("SHA-1");
            byte[] hash = sha1.digest((key + WEBSOCKET_GUID).getBytes(StandardCharsets.UTF_8));
            return Base64.getEncoder().encodeToString(hash);
        }

        @Override
        public void close() throws IOException {
            serverSocket.close();
            Socket socket = connection;
            if (socket != null) {
                socket.close();
            }
        }
    }
}
