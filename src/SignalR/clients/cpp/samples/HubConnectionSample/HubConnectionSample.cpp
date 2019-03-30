// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"

#include <iostream>
#include <sstream>
#include "hub_connection.h"
#include "log_writer.h"
#include <future>

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
    connection.invoke("Send", args, [](const web::json::value& value, std::exception_ptr exception)
    {
        try
        {
            if (exception)
            {
                std::rethrow_exception(exception);
            }

            ucout << U("Received: ") << value.serialize() << std::endl;
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
    connection.on("Send", [](const web::json::value & m)
    {
        ucout << std::endl << m.at(0).as_string() << /*U(" wrote:") << m.at(1).as_string() <<*/ std::endl << U("Enter your message: ");
    });

    std::promise<void> task;
    connection.start([&connection, &task](std::exception_ptr exception)
    {
        if (exception)
        {
            try
            {
                std::rethrow_exception(exception);
            }
            catch (const std::exception & ex)
            {
                ucout << U("exception when starting connection: ") << ex.what() << std::endl;
            }
            task.set_value();
            return;
        }

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

        connection.stop([&task](std::exception_ptr exception)
        {
            try
            {
                if (exception)
                {
                    std::rethrow_exception(exception);
                }

                ucout << U("connection stopped successfully") << std::endl;
            }
            catch (const std::exception & e)
            {
                ucout << U("exception when stopping connection: ") << e.what() << std::endl;
            }

            task.set_value();
        });
    });

    task.get_future().get();
}

int main()
{
    chat();

    return 0;
}
