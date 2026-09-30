// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.ServerSocket;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.concurrent.TimeUnit;

import org.junit.jupiter.api.Test;

import io.reactivex.rxjava3.observers.TestObserver;

public class DefaultHttpClientTest {
    private static final String NEGOTIATE_BODY = "{\"connectionId\":\"bVOiRPONXYEq3sPMCVqCuA\",\"connectionToken\":\"connection-token\","
            + "\"negotiateVersion\":1,\"availableTransports\":[{\"transport\":\"WebSockets\",\"transferFormats\":[\"Text\",\"Binary\"]}]}";

    @Test
    public void requestCompletesWhenResponseIsWellFormed() throws Exception {
        try (RawResponseServer server = RawResponseServer.start(ResponseMode.COMPLETE);
             DefaultHttpClient client = new DefaultHttpClient(null)) {
            TestObserver<HttpResponse> observer = client.get(server.getUrl()).test();

            assertTrue(observer.await(30, TimeUnit.SECONDS), "The request never terminated.");
            observer.assertNoErrors();
            assertEquals(200, observer.values().get(0).getStatusCode());
        }
    }

    // Reading the response body can fail after OkHttp has already handed us the response. OkHttp does not
    // call onFailure in that case, so the client has to terminate the Single itself.
    @Test
    public void requestFailsWhenResponseBodyIsTruncated() throws Exception {
        try (RawResponseServer server = RawResponseServer.start(ResponseMode.TRUNCATED_BODY);
             DefaultHttpClient client = new DefaultHttpClient(null)) {
            TestObserver<HttpResponse> observer = client.get(server.getUrl()).test();

            assertTrue(observer.await(30, TimeUnit.SECONDS), "The request never terminated.");
            observer.assertError(IOException.class);
        }
    }

    // Regression test for a truncated negotiate response leaving HubConnection.start() pending forever,
    // which also made stop() hang because it waits on the start task.
    @Test
    public void startFailsWhenNegotiateResponseIsTruncated() throws Exception {
        try (RawResponseServer server = RawResponseServer.start(ResponseMode.TRUNCATED_BODY)) {
            HubConnection hubConnection = HubConnectionBuilder.create(server.getUrl()).build();

            // Await the start task directly rather than applying an Rx timeout to it, otherwise this would
            // still pass while start() itself hangs.
            TestObserver<Void> observer = hubConnection.start().test();

            assertTrue(observer.await(30, TimeUnit.SECONDS), "start() never terminated.");
            observer.assertError(IOException.class);
            assertEquals(HubConnectionState.DISCONNECTED, hubConnection.getConnectionState());

            hubConnection.stop().timeout(30, TimeUnit.SECONDS).blockingAwait();

            // Release the underlying HttpClient. This is deliberately not a try-with-resources or a finally
            // block: close() waits on the start task without a timeout, so closing after a failed assertion
            // would hang the test run instead of reporting the failure. Getting here means stop() already
            // completed, which leaves close() with no work to wait for.
            hubConnection.close();
        }
    }

    private enum ResponseMode {
        COMPLETE,
        TRUNCATED_BODY
    }

    /**
     * A minimal HTTP server that writes a canned raw response so tests can control exactly what reaches the client.
     */
    private static final class RawResponseServer implements AutoCloseable {
        private final ServerSocket serverSocket;
        private final Thread acceptThread;

        private RawResponseServer(ServerSocket serverSocket, ResponseMode mode) {
            this.serverSocket = serverSocket;
            this.acceptThread = new Thread(() -> {
                while (!serverSocket.isClosed()) {
                    try {
                        Socket socket = serverSocket.accept();
                        new Thread(() -> respond(socket, mode)).start();
                    } catch (Exception ex) {
                        return;
                    }
                }
            });
            this.acceptThread.setDaemon(true);
        }

        public static RawResponseServer start(ResponseMode mode) throws Exception {
            RawResponseServer server = new RawResponseServer(new ServerSocket(0), mode);
            server.acceptThread.start();
            return server;
        }

        public String getUrl() {
            return "http://localhost:" + serverSocket.getLocalPort() + "/hub";
        }

        private static void respond(Socket socket, ResponseMode mode) {
            try (Socket toClose = socket) {
                InputStream input = socket.getInputStream();
                byte[] buffer = new byte[8192];
                if (input.read(buffer) <= 0) {
                    return;
                }

                byte[] body = NEGOTIATE_BODY.getBytes(StandardCharsets.UTF_8);
                // Always advertise the full length, but only write part of the body when truncating so the
                // client sees the connection end in the middle of the response.
                int bytesToWrite = mode == ResponseMode.TRUNCATED_BODY ? body.length / 2 : body.length;
                String headers = "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: " + body.length + "\r\n\r\n";

                OutputStream output = socket.getOutputStream();
                output.write(headers.getBytes(StandardCharsets.UTF_8));
                output.write(body, 0, bytesToWrite);
                output.flush();
            } catch (Exception ex) {
                // The client closing first is expected for some of these tests.
            }
        }

        @Override
        public void close() throws Exception {
            serverSocket.close();
        }
    }
}
