// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Mvc;
using ModelBindingWebSite.Services;

namespace ModelBindingWebSite.ViewModels
{
    public class VehicleWithDealerViewModel : IValidatableObject
    {
        [Required]
        public DealerViewModel Dealer { get; set; }

        [Required]
        [FromBody]
        public VehicleViewModel Vehicle { get; set; }

        [FromServices]
        public ILocationService LocationService { get; set; }

        [FromHeader(Name = "X-TrackingId")]
        public string TrackingId { get; set; } = "default-tracking-id";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!LocationService.IsValidMakeForRegion(Vehicle.Make, Dealer.Location))
            {
                yield return new ValidationResult("Make is invalid for region.");
            }
        }

        public void Update()
        {
            LocationService.Update(this);
        }
    }
}