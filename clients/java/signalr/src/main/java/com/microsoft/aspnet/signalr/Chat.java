// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

package com.microsoft.aspnet.signalr;

import java.util.Scanner;

public class Chat {
    public static void main(String[] args) throws Exception {
            System.out.println("Enter the URL of the SignalR Chat you want to join");
            Scanner reader = new Scanner(System.in);  // Reading from System.in
            String input;
            input = reader.nextLine();

            HubConnection hubConnection = new HubConnectionBuilder()
                    .withUrl(input)
                    .configureLogging(LogLevel.Information).build();

            hubConnection.on("Send", (message) -> {
                System.out.println("REGISTERED HANDLER: " + message);
            }, String.class);

            //This is a blocking call
            hubConnection.start();

            while (!input.equals("leave")){
                // Scans the next token of the input as an int.
                input = reader.nextLine();
                hubConnection.send("Send", input);
            }

            hubConnection.stop();
    }
}
