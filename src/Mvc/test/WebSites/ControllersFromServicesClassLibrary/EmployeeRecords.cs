// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace ControllersFromServicesClassLibrary;

public class EmployeeRecords : Controller
{
    [HttpPut("/employee/update_records")]
    public IActionResult UpdateRecords(string recordId)
    {
        return Content("Updated record " + recordId);
    }

    [HttpPost]
    // This action uses conventional routing.
    public IActionResult Save(string id)
    {
        return Content("Saved record employee #" + id);
    }
}
