// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace ModelBindingWebSite.Models
{
    public class Country
    {
        public string Name { get; set; }

        public City[] Cities { get; set; }

        public int[] StateCodes { get; set; }
    }

    public class City
    {
        public string CityName { get; set; }

        public string CityCode { get; set; }
    }
}