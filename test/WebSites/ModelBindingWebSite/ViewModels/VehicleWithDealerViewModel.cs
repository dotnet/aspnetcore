// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite.ViewModels
{
    public class VehicleWithDealerViewModel : IValidatableObject
    {
        [Required]
        public DealerViewModel Dealer { get; set; }

        [Required]
        [FromBody]
        public VehicleViewModel Vehicle { get; set; }

        [FromHeader(Name = "X-TrackingId")]
        public string TrackingId { get; set; } = "default-tracking-id";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!IsValidMakeForRegion(Vehicle.Make, Dealer.Location))
            {
                yield return new ValidationResult("Make is invalid for region.");
            }
        }

        public bool IsValidMakeForRegion(string make, string region)
        {
            switch (make)
            {
                case "Acme":
                    return region == "NW" || "region" == "South Central";
                case "FastCars":
                    return region != "Central";
            }

            return true;
        }
    }
}