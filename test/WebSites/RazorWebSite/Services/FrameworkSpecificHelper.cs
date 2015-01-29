// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace RazorWebSite
{
    public class FrameworkSpecificHelper
    {
        public string ExecuteOperation()
        {
#if ASPNET50
            return "This method is running from ASPNET50";
#elif ASPNETCORE50
            return "This method is running from ASPNETCORE50";
#endif
        }

#if ASPNET50_CUSTOM_DEFINE
        public string ExecuteAspNet50Operation()
        {
            return "This method is only defined in ASPNET50";
        }
#endif

#if ASPNETCORE50_CUSTOM_DEFINE
        public string ExecuteAspNetCore50Operation()
        {
            return "This method is only defined in ASPNETCORE50";
        }
#endif
    }
}