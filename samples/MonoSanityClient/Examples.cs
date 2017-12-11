// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace MonoSanityClient
{
    public class Examples
    {
        public static string AddNumbers(int a, int b)
            => (a + b).ToString();

        public static string RepeatString(string str, int count)
        {
            var result = new StringBuilder();

            for (var i = 0; i < count; i++)
            {
                result.Append(str);
            }

            return result.ToString();
        }

        public static void TriggerException(string message)
        {
            throw new InvalidOperationException(message);
        }
    }
}
