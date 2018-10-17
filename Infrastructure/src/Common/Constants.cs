// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Common
{
    public static class Constants
    {
        public const string VSTestPrefix = "VSTest: ";

        /// <summary>
        /// This property keeps the various providers from making changes to their data sources when testing things out.
        /// </summary>
        public static bool BeQuiet;
    }
}
