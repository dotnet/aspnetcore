// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace RazorWebSite
{
    public class FrameworkSpecificHelper
    {
        public string ExecuteOperation()
        {
#if NET452
            return "This method is running from NET452";
#elif NETCOREAPP1_1
            return "This method is running from NETCOREAPP1_1";
#endif
        }

#if NET452_CUSTOM_DEFINE
        public string ExecuteNet452Operation()
        {
            return "This method is only defined in NET452";
        }
#endif

#if NETCOREAPP1_1_CUSTOM_DEFINE
        public string ExecuteNetCoreApp1_1Operation()
        {
            return "This method is only defined in NETCOREAPP1_1";
        }
#endif
    }
}
