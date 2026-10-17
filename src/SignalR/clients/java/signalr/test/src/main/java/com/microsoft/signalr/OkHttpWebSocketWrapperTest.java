// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.net.InetAddress;
import java.net.ServerSocket;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.TimeUnit;

import org.junit.jupiter.api.Test;

class OkHttpWebSocketWrapperTest {
    @Test
    public void nonSwitchingProtocolsResponseThrowsHttpRequestException() throws Exception {
        try (ServerSocket server = new ServerSocket(0, 1, InetAddress.getByName("127.0.0.1"))) {
            CompletableFuture<Void> serverResponse = CompletableFuture.runAsync(() -> respondWithUnauthorized(server));

            HubConnection hubConnection = HubConnectionBuilder
                .create("http://127.0.0.1:" + server.getLocalPort() + "/hub")
                .withTransport(TransportEnum.WEBSOCKETS)
                .shouldSkipNegotiate(true)
                .build();

            HttpRequestException exception = assertThrows(HttpRequestException.class,
                () -> hubConnection.start().timeout(30, TimeUnit.SECONDS).blockingAwait());

            assertEquals("Unexpected status code returned from WebSocket handshake: 401 Unauthorized.", exception.getMessage());
            assertEquals(401, exception.getStatusCode());
            serverResponse.get(30, TimeUnit.SECONDS);
        }
    }

    private static void respondWithUnauthorized(ServerSocket server) {
        try (Socket socket = server.accept()) {
            BufferedReader reader = new BufferedReader(
                new InputStreamReader(socket.getInputStream(), StandardCharsets.US_ASCII));
            String requestLine;
            while ((requestLine = reader.readLine()) != null && !requestLine.isEmpty()) {
                // Read the complete request before sending the response.
            }

            byte[] response = "HTTP/1.1 401 Unauthorized\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"
                .getBytes(StandardCharsets.US_ASCII);
            socket.getOutputStream().write(response);
            socket.getOutputStream().flush();
        } catch (Exception exception) {
            throw new RuntimeException(exception);
        }
    }
}
