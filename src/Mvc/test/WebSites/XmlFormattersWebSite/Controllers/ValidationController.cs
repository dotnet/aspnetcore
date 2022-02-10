// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace XmlFormattersWebSite;

public class ValidationController : Controller
{
    public IActionResult CreateStore([FromBody] Store store)
    {
        // We want to verify that 'store' is model bound and also that the
        // model state has the errors we are expecting.
        return new ObjectResult(new ModelBindingInfo()
        {
            Store = store,
            ModelStateErrorMessages = GetModelStateErrorMessages(ModelState)
        });
    }

    // Cannot use 'SerializableError' here as 'RequiredAttribute' validation errors are added as exceptions
    // into the model state dictionary and 'SerializableError' sanitizes exceptions with generic error message.
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
                string errorMessage = null;
                foreach (var modelError in errors)
                {
                    if (string.IsNullOrEmpty(modelError.ErrorMessage))
                    {
                        if (modelError.Exception != null)
                        {
                            errorMessage = modelError.Exception.Message;
                        }
                    }
                    else
                    {
                        errorMessage = modelError.ErrorMessage;
                    }

                    if (errorMessage != null)
                    {
                        allErrorMessages.Add(string.Format(CultureInfo.InvariantCulture, "{0}:{1}", key, errorMessage));
                    }
                }
            }
        }

        return allErrorMessages;
    }
}

public class ModelBindingInfo
{
    public Store Store { get; set; }

    public List<string> ModelStateErrorMessages { get; set; }
}
