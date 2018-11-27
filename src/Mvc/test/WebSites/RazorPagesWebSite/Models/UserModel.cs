// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace RazorPagesWebSite
{
    public class UserModel : IUserModel
    {
        [Required]
        public string Name { get; set; }

        [Range(0, 99)]
        public int Age { get; set; }

        public override string ToString()
        {
            return $"Name = {Name}, Age = {Age}";
        }
    }
}
