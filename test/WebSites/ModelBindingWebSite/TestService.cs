// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace ModelBindingWebSite
{
    public class TestService :  ITestService
    {
        [Required]
        public string NeverBound { get; set; }

        public bool Test()
        {
            return true;
        }
    }
}
