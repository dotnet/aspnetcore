// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace RazorWebSite
{
    public class FrameworkSpecificHelper
    {
        public string ExecuteOperation()
        {
#if NET451
            return "This method is running from NET451";
#elif DNXCORE50
            return "This method is running from DNXCORE50";
#endif
        }

#if NET451_CUSTOM_DEFINE
        public string ExecuteNet451Operation()
        {
            return "This method is only defined in NET451";
        }
#endif

#if DNXCORE50_CUSTOM_DEFINE
        public string ExecuteDnxCore50Operation()
        {
            return "This method is only defined in DNXCORE50";
        }
#endif
    }
}
