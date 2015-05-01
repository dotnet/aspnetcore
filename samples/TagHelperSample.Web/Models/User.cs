// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace TagHelperSample.Web.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Blurb { get; set; }

        [DataType(DataType.Date)]
        public DateTimeOffset DateOfBirth { get; set; }

        public int YearsEmployeed { get; set; }
    }
}