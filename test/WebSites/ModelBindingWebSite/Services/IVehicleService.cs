// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using ModelBindingWebSite.ViewModels;

namespace ModelBindingWebSite.Services
{
    public interface IVehicleService
    {
        void Update(int id, VehicleViewModel vehicle, string trackingId);
    }
}