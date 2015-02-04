// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace ModelBindingWebSite
{
    public class Address
    {
        public int Street { get; set; }
        public string State { get; set; }

        [Range(10000, 99999)]
        public int Zip { get; set; }

        public Country Country { get; set; }
    }
}