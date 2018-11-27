// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace RazorWebSite
{
    public class FrameworkSpecificHelper
    {
        public string ExecuteOperation()
        {
            return "This method is running from NETCOREAPP2_0";
        }

#if NETCOREAPP2_0_CUSTOM_DEFINE || NETCOREAPP2_1_CUSTOM_DEFINE
        public string ExecuteNetCoreApp2_0Operation()
        {
            return "This method is only defined in NETCOREAPP2_0";
        }
#endif
    }
}
