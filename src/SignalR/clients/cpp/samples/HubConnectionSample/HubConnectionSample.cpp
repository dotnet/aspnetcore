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
    virtual void __cdecl write(const utility::string_t & entry) override
    {
        //std::cout << utility::conversions::to_utf8string(entry) << std::endl;
    }
};

void send_message(signalr::hub_connection& connection, const utility::string_t& name, const utility::string_t& message)
{
    web::json::value args{};
    args[0] = web::json::value::string(name);
    args[1] = web::json::value(message);

    // if you get an internal compiler error uncomment the lambda below or install VS Update 4
    connection.invoke(U("Invoke"), args/*, [](const web::json::value&){}*/)
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

void chat(const utility::string_t& name)
{
    signalr::hub_connection connection(U("http://localhost:5000/default"), U(""), signalr::trace_level::all, std::make_shared<logger>());
    connection.on(U("Send"), [](const web::json::value& m)
    {
        ucout << std::endl << m.at(0).as_string() << /*U(" wrote:") << m.at(1).as_string() <<*/ std::endl << U("Enter your message: ");
    });

    connection.start()
        .then([&connection, name]()
        {
            ucout << U("Enter your message:");
            for (;;)
            {
                utility::string_t message;
                std::getline(ucin, message);

                if (message == U(":q"))
                {
                    break;
                }

                send_message(connection, name, message);
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
    ucout << U("Enter your name: ");
    utility::string_t name;
    std::getline(ucin, name);

    chat(name);

    return 0;
}
