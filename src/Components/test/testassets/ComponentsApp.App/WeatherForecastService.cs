// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace ComponentsApp
{
    public abstract class WeatherForecastService
    {
        public abstract Task<WeatherForecast[]> GetForecastAsync(DateTime startDate);
    }
}
