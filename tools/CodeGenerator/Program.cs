// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace CodeGenerator
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var text0 = KnownHeaders.GeneratedFile();
            var text1 = FrameFeatureCollection.GeneratedFile();

            if (args.Length == 1)
            {
                var existing = File.Exists(args[0]) ? File.ReadAllText(args[0]) : "";
                if (!string.Equals(text0, existing))
                {
                    File.WriteAllText(args[0], text0);
                }
            }
            else if (args.Length == 2)
            {
                var existing0 = File.Exists(args[0]) ? File.ReadAllText(args[0]) : "";
                if (!string.Equals(text0, existing0))
                {
                    File.WriteAllText(args[0], text0);
                }

                var existing1 = File.Exists(args[1]) ? File.ReadAllText(args[1]) : "";
                if (!string.Equals(text1, existing1))
                {
                    File.WriteAllText(args[1], text1);
                }
            }
            else
            {
                Console.WriteLine(text0);
            }
            return 0;
        }
    }
}
