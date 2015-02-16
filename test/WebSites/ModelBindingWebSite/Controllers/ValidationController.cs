// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace ModelBindingWebSite.Controllers
{
    [Route("Validation/[Action]")]
    public class ValidationController : Controller
    {
        public bool SkipValidation(Resident resident)
        {
            return ModelState.IsValid;
        }

        public bool AvoidRecursive(SelfishPerson selfishPerson)
        {
            return ModelState.IsValid;
        }

        public IActionResult CreateRectangle([FromBody] Rectangle rectangle)
        {
            if (!ModelState.IsValid)
            {
                return new ObjectResult(GetModelStateErrorMessages(ModelState)) { StatusCode = 400 };
            }

            return new ObjectResult(rectangle);
        }

        private IEnumerable<string> GetModelStateErrorMessages(ModelStateDictionary modelStateDictionary)
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

    public class SelfishPerson
    {
        public string Name { get; set; }
        public SelfishPerson MySelf { get { return this; } }
    }
}