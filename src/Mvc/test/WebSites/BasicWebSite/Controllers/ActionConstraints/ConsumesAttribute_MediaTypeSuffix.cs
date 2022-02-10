// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.ActionConstraints;

[Route("ConsumesAttribute_MediaTypeSuffix/[action]")]
public class ConsumesAttribute_MediaTypeSuffix : Controller
{
    [Consumes("application/vnd.example+json")]
    public Product CreateProduct([FromBody] Product_Json jsonInput)
    {
        // To show that we selected the correct method (and not just the
        // correct input formatter), produce method-specific output.
        jsonInput.SampleString = "Read from JSON: " + jsonInput.SampleString;
        return jsonInput;
    }

    [Consumes("application/vnd.example+xml")]
    public Product CreateProduct([FromBody] Product_Xml xmlInput)
    {
        // To show that we selected the correct method (and not just the
        // correct input formatter), produce method-specific output.
        xmlInput.SampleString = "Read from XML: " + xmlInput.SampleString;
        return xmlInput;
    }
}
