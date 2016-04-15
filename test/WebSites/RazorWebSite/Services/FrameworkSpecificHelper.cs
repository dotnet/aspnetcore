// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace RazorWebSite
{
    public class FrameworkSpecificHelper
    {
        public string ExecuteOperation()
        {
#if NET451 || DNX451
            return "This method is running from NET451";
#elif NETCOREAPP1_0
            return "This method is running from NETCOREAPP1_0";
#endif
        }

#if NET451_CUSTOM_DEFINE
        public string ExecuteNet451Operation()
        {
            return "This method is only defined in NET451";
        }
#endif

#if NETCOREAPP1_0_CUSTOM_DEFINE
        public string ExecuteNetCoreApp1_0Operation()
        {
            return "This method is only defined in NETCOREAPP1_0";
        }
#endif
    }
}
