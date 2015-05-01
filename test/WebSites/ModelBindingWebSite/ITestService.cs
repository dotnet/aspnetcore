// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace ModelBindingWebSite
{
    public interface ITestService
    {
        [Required]
        string NeverBound { get; set; }

        bool Test();
    }
}
