// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace BasicViews
{
    public class Person
    {
        public int Id { get; set; }

        [StringLength(27, MinimumLength = 2)]
        public string Name { get; set; }

        [Range(10, 54)]
        public int Age { get; set; }

        public DateTimeOffset BirthDate { get; set; }
    }
}
