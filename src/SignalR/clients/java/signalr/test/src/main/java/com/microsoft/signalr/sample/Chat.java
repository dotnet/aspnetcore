// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

package com.microsoft.signalr.sample;

import java.util.Scanner;

import com.microsoft.signalr.HubConnection;
import com.microsoft.signalr.HubConnectionBuilder;

public class Chat {
    public static void main(final String[] args) throws Exception {
        System.out.println("Enter the URL of the SignalR Chat you want to join");
        final Scanner reader = new Scanner(System.in); // Reading from System.in
        final String input = reader.nextLine();

        try (HubConnection hubConnection = HubConnectionBuilder.create(input).build()) {
            hubConnection.on("Send", (message) -> {
                System.out.println(message);
            }, String.class);

            hubConnection.onClosed((ex) -> {
                if (ex != null) {
                    System.out.printf("There was an error: %s", ex.getMessage());
                }
            });

            //This is a blocking call
            hubConnection.start().blockingAwait();

            String message = "";
            while (!message.equals("leave")) {
                // Scans the next token of the input as an int.
                message = reader.nextLine();
                hubConnection.send("Send", "Java", message);
            }

            hubConnection.stop().blockingAwait();
        }
    }
}
