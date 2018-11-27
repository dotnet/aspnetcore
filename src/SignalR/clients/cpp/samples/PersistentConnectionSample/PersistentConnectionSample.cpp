// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "signalrclient\connection.h"
#include <iostream>

void send_message(signalr::connection &connection, const utility::string_t& message)
{
    connection.send(message)
        .then([](pplx::task<void> send_task)  // fire and forget but we need to observe exceptions
    {
        try
        {
            send_task.get();
        }
        catch (const std::exception &e)
        {
            ucout << U("Error while sending data: ") << e.what();
        }
    });
}

int main()
{
    signalr::connection connection{ U("http://localhost:34281/echo") };
    connection.set_message_received([](const utility::string_t& m)
    {
        ucout << U("Message received:") << m << std::endl << U("Enter message: ");
    });

    connection.start()
        .then([&connection]() // fine to capture by reference - we are blocking so it is guaranteed to be valid
        {
            for (;;)
            {
                utility::string_t message;
                std::getline(ucin, message);

                if (message == U(":q"))
                {
                    break;
                }

                send_message(connection, message);
            }

            return connection.stop();
        })
        .then([](pplx::task<void> stop_task)
        {
            try
            {
                stop_task.get();
                ucout << U("connection stopped successfully") << std::endl;
            }
            catch (const std::exception &e)
            {
                ucout << U("exception when starting or closing connection: ") << e.what() << std::endl;
            }
        }).get();

    return 0;
}
