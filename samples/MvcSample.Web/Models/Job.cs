// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace MvcSample.Web
{
    public class Job
    {
        [Required]
        public string JobTitle { get; set; }

        [Required]
        public string EmployerName { get; set; }

        [Required]
        public int Years { get; set; }
    }
}