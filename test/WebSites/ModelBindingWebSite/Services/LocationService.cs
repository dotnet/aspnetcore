// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using ModelBindingWebSite.ViewModels;

namespace ModelBindingWebSite.Services
{
    public class LocationService : ILocationService
    {
        public bool Update(VehicleWithDealerViewModel viewModel)
        {
            return true;
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