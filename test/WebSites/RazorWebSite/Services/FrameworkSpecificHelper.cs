// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace RazorWebSite
{
    public class FrameworkSpecificHelper
    {
        public string ExecuteOperation()
        {
#if DNX451
            return "This method is running from DNX451";
#elif DNXCORE50
            return "This method is running from DNXCORE50";
#endif
        }

#if DNX451_CUSTOM_DEFINE
        public string ExecuteDnx451Operation()
        {
            return "This method is only defined in DNX451";
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
