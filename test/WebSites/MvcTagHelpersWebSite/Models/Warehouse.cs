// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace MvcTagHelpersWebSite.Models
{
    public class Warehouse
    {
        [MinLength(2)]
        public string City
        {
            get;
            set;
        }

        [Range(1, 100)]
        public int Product
        {
            get;
            set;
        }

        public Employee Employee
        {
            get;
            set;
        }
    }
}