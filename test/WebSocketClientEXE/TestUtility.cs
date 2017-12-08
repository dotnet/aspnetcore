// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace AspNetCoreModule.Test.Framework
{
    public class TestUtility
    {
        public static void LogInformation(string format, params object[] parameters)
        {
            Console.WriteLine(format, parameters);
        }
    }   
}