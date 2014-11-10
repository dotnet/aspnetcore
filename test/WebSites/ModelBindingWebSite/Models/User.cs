// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace ModelBindingWebSite
{
    public class User
    {
        [Required]
        public int Id { get; set; }
        public int Key { get; set; }

        [Required]
        public string RegisterationMonth { get; set; }
        public string UserName { get; set; }

        public Address Address { get; set; }
    }
}