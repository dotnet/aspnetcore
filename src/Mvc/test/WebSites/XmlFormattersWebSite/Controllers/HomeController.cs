// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace XmlFormattersWebSite;

public class HomeController : Controller
{
    [HttpPost]
    public IActionResult Index([FromBody] DummyClass dummyObject)
    {
        if (!ModelState.IsValid)
        {
            return new ObjectResult(GetModelStateErrorMessages(ModelState))
            {
                StatusCode = 400
            };
        }

        return Content(dummyObject.SampleInt.ToString(CultureInfo.InvariantCulture));
    }

    // Cannot use 'SerializableError' here as it sanitizes exceptions in model state with generic error message.
    // Since the tests need to verify the messages, we are doing the following.
    private List<string> GetModelStateErrorMessages(ModelStateDictionary modelStateDictionary)
    {
        var allErrorMessages = new List<string>();
        foreach (var keyModelStatePair in modelStateDictionary)
        {
            var key = keyModelStatePair.Key;
            var errors = keyModelStatePair.Value.Errors;
            if (errors != null && errors.Count > 0)
            {
                allErrorMessages.Add(
                    string.Join(
                        ",",
                        errors.Select(modelError => $"ErrorMessage:{modelError.ErrorMessage};Exception:{modelError.Exception}")));
            }
        }

        return allErrorMessages;
    }
}
