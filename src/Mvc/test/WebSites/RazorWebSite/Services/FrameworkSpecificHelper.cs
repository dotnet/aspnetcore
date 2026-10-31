// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace RazorWebSite;

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
