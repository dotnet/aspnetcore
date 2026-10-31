// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ControllersFromServicesClassLibrary;

public class TimeScheduleController
{
    [HttpGet("/schedule/{id:int}")]
    public IActionResult GetSchedule(int id)
    {
        return new ContentResult { Content = "No schedules available for " + id };
    }
}
