// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"

#include <iostream>
#include <sstream>
#include "hub_connection.h"
#include "log_writer.h"

class logger : public signalr::log_writer
{
    // Inherited via log_writer
    virtual void __cdecl write(const std::string & entry) override
    {
        //std::cout << utility::conversions::to_utf8string(entry) << std::endl;
    }
};

void send_message(signalr::hub_connection& connection, const std::string& message)
{
    web::json::value args{};
    args[0] = web::json::value(utility::conversions::to_string_t(message));

    // if you get an internal compiler error uncomment the lambda below or install VS Update 4
    connection.invoke("Send", args)
        .then([](pplx::task<web::json::value> invoke_task)  // fire and forget but we need to observe exceptions
    {
        try
        {
            auto val = invoke_task.get();
            ucout << U("Received: ") << val.serialize() << std::endl;
        }
        catch (const std::exception &e)
        {
            ucout << U("Error while sending data: ") << e.what() << std::endl;
        }
    });
}

void chat()
{
    signalr::hub_connection connection("http://localhost:5000/default", signalr::trace_level::all, std::make_shared<logger>());
    connection.on("Send", [](const web::json::value& m)
    {
        ucout << std::endl << m.at(0).as_string() << /*U(" wrote:") << m.at(1).as_string() <<*/ std::endl << U("Enter your message: ");
    });

    connection.start()
        .then([&connection]()
        {
            ucout << U("Enter your message:");
            for (;;)
            {
                std::string message;
                std::getline(std::cin, message);

                if (message == ":q")
                {
                    break;
                }

                send_message(connection, message);
            }
        })
        .then([&connection]() // fine to capture by reference - we are blocking so it is guaranteed to be valid
        {
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
                ucout << U("exception when starting or stopping connection: ") << e.what() << std::endl;
            }
        }).get();
}

int main()
{
    chat();

    return 0;
}
