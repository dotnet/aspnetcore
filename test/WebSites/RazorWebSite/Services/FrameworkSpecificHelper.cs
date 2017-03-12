// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace RazorWebSite
{
    public class FrameworkSpecificHelper
    {
        public string ExecuteOperation()
        {
#if NET46
            return "This method is running from NET46";
#elif NETCOREAPP2_0
            return "This method is running from NETCOREAPP2_0";
#else
#error the target framework needs to be updated.                    
#endif            
        }

#if NET46_CUSTOM_DEFINE
        public string ExecuteNET46Operation()
        {
            return "This method is only defined in NET46";
        }
#endif

#if NETCOREAPP2_0_CUSTOM_DEFINE
        public string ExecuteNetCoreApp2_0Operation()
        {
            return "This method is only defined in NETCOREAPP2_0";
        }
#endif
    }
}
