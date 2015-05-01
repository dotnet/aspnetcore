// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace ModelBindingWebSite.ViewModels
{
    public class DealerViewModel
    {
        private const string DefaultCustomerServiceNumber = "999-99-0000";

        public int Id { get; set; }

        public string Name { get; set; }

        [Required]
        public string Location { get; set; }

        [DataType(DataType.PhoneNumber)]
        public string Phone { get; set; } = DefaultCustomerServiceNumber;
    }
}