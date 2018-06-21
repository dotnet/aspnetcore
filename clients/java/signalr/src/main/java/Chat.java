// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import com.google.gson.JsonArray;

import java.net.URISyntaxException;
import java.util.Scanner;

public class Chat {
    public static void main(String[] args) throws URISyntaxException, InterruptedException {
            System.out.println("Enter the URL of the SignalR Chat you want to join");
            Scanner reader = new Scanner(System.in);  // Reading from System.in
            String input;
            input = reader.nextLine();
            HubConnection hubConnection = new HubConnection(input);

            hubConnection.On("Send", (message) -> {
                String newMessage = ((JsonArray) message).get(0).getAsString();
                System.out.println("REGISTERED HANDLER: " + newMessage);
            });

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
