// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MvcSample.Web.Models
{
    [DisplayColumn("Name")]
    public class User
    {
        public User()
        {
            OwnedAddresses = new List<string>();
            ParentsAges = new List<int>();
        }

        [Required]
        [MinLength(4)]
        public string Name { get; set; }
        public string Address { get; set; }
        [Range(27, 70)]
        public int Age { get; set; }
        public decimal GPA { get; set; }
        public User Dependent { get; set; }
        public bool Alive { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = true, NullDisplayText = "You can explain about your profession")]
        public string Profession { get; set; }
        public string About { get; set; }
        public string Log { get; set; }
        public IEnumerable<string> OwnedAddresses { get; private set; }

        // This does not bind correctly. Only gets highest value.
        public List<int> ParentsAges { get; private set; }
    }
}