// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace XmlFormattersWebSite
{
    public class HomeController : Controller
    {
        [HttpPost]
        public IActionResult Index([FromBody]DummyClass dummyObject)
        {
            if (!ModelState.IsValid)
            {
                return new ObjectResult(GetModelStateErrorMessages(ModelState))
                {
                    StatusCode = 400
                };
            }

            return Content(dummyObject.SampleInt.ToString());
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
                            allErrorMessages.Add(string.Format("{0}:{1}", key, errorMessage));
                        }
                    }
                }
            }

            return allErrorMessages;
        }
    }
}