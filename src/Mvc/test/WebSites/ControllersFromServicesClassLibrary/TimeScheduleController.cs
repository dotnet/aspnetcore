// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ControllersFromServicesClassLibrary
{
    public class TimeScheduleController
    {
        [HttpGet("/schedule/{id:int}")]
        public IActionResult GetSchedule(int id)
        {
            return new ContentResult { Content = "No schedules available for " + id };
        }
    }
}
