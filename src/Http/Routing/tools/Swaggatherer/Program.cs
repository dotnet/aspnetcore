// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Swaggatherer
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var application = new SwaggathererApplication();
            application.Execute(args);
        }
    }
}
