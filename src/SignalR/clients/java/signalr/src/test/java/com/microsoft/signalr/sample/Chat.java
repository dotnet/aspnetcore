// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.signalr.sample;

import java.util.Scanner;

import com.microsoft.signalr.HubConnection;
import com.microsoft.signalr.HubConnectionBuilder;


public class Chat {
    public static void main(String[] args) {
        System.out.println("Enter the URL of the SignalR Chat you want to join");
        Scanner reader = new Scanner(System.in);  // Reading from System.in
        String input = reader.nextLine();

        System.out.print("Enter your name:");
        String enteredName = reader.nextLine();

        HubConnection hubConnection = HubConnectionBuilder.create(input).build();

        hubConnection.on("Send", (name, message) -> {
            System.out.println(name + ": " + message);
        }, String.class, String.class);

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
            hubConnection.send("Send", enteredName, message);
        }

        hubConnection.stop();
    }
}
