// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ModelBindingWebSite.ViewModels
{
    public class VehicleViewModel : IValidatableObject
    {
        // Placed using default Order (10000).
        [Required]
        [StringLength(8)]
        public string Vin { get; set; }

        [Display(Order = 1)]
        public string Make { get; set; }

        [Display(Order = 0)]
        public string Model { get; set; }

        // Placed using default Order (10000).
        [Range(1980, 2034)]
        [CustomValidation(typeof(VehicleViewModel), nameof(ValidateYear))]
        public int Year { get; set; }

        [Display(Order = 20000)]
        [Required]
        [MaxLength(10)]
        public DateTimeOffset[] InspectedDates { get; set; }

        [Display(Order = 20000)]
        public string LastUpdatedTrackingId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (InspectedDates.Any(d => d.Year > Year))
            {
                yield return new ValidationResult("Inspection date cannot be later than year of manufacture.",
                                                  new[] { nameof(InspectedDates) });
            }
        }

        public static ValidationResult ValidateYear(int year)
        {
            if (year > DateTime.UtcNow.Year)
            {
                return new ValidationResult("Year is invalid");
            }

            return ValidationResult.Success;
        }
    }
}