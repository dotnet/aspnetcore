// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace MvcTagHelpersWebSite.Models
{
    public class Employee : Person
    {
        public string Address
        {
            get;
            set;
        }

        public string OfficeNumber
        {
            get;
            set;
        }

        public bool Remote
        {
            get;
            set;
        }
    }
}