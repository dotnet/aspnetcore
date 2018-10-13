// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;

namespace Microsoft.Web.Utility
{
    internal static class MySqlConnector
    {
        public static string[] HardCodedAssemblyVersions
        {
            get
            {
                return new string[]
                {
                    "MySql.Data, Version=6.5.4.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d",
                    "MySql.Data, Version=6.4.4.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d",
                    "MySql.Data, Version=6.3.7.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d",
                    "MySql.Data, Version=6.2.3.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d",
                    "MySql.Data, Version=6.0.4.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d",
                    "MySql.Data, Version=6.0.3.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d",
                    "MySql.Data, Version=5.2.6.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d",
                    "MySql.Data, Version=5.2.5.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d",
                };
            }
        }
    }
}