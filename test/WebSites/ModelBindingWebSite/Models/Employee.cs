// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite.Models
{
    public class Employee : Person
    {
        public string Department { get; set; }

        public string Location { get; set; }

        [FromQuery(Name = "EmployeeId")]
        [Range(1, 10000)]
        public int Id { get; set; }

        [FromRoute(Name = "EmployeeTaxId")]
        public int TaxId { get; set; }

        [FromForm(Name = "EmployeeSSN")]
        public string SSN { get; set; }

        [ModelBinder(Name = "Alias")]
        public string EmailAlias { get; set; }
    }
}