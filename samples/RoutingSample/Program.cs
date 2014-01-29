﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.Owin.Hosting;

namespace RoutingSample
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            string url = "http://localhost:30000/";
            using (WebApp.Start<Startup>(url))
            {
                Console.WriteLine("Listening on {0}", url);
                Console.WriteLine("Press ENTER to quit");

                Console.ReadLine();
            }
        }
    }
}
