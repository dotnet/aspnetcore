// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using ModelBindingWebSite.Models;

namespace ModelBindingWebSite.Controllers
{
    [Route("Validation/[Action]")]
    public class ValidationController : Controller
    {
        [FromServices]
        public ITestService ControllerService { get; set; }

        public object AvoidRecursive(SelfishPerson selfishPerson)
        {
            return new SerializableModelStateDictionary(ModelState);
        }

        public object DoNotValidateParameter([FromServices] ITestService service)
        {
            return ModelState;
        }
    }

    public class SerializableModelStateDictionary : Dictionary<string, Entry>
    {
        public bool IsValid { get; set; }

        public int ErrorCount { get; set; }

        public SerializableModelStateDictionary(ModelStateDictionary modelState)
        {
            var errorCount = 0;
            foreach (var keyModelStatePair in modelState)
            {
                var key = keyModelStatePair.Key;
                var value = keyModelStatePair.Value;
                errorCount += value.Errors.Count;
                var entry = new Entry()
                {
                    Errors = value.Errors,
                    RawValue = value.Value.RawValue,
                    AttemptedValue = value.Value.AttemptedValue,
                    ValidationState = value.ValidationState
                };

                Add(key, entry);
            }

            IsValid = modelState.IsValid;
            ErrorCount = errorCount;
        }
    }

    public class Entry
    {
        public ModelValidationState ValidationState { get; set; }

        public ModelErrorCollection Errors { get; set; }

        public object RawValue { get; set; }

        public string AttemptedValue { get; set; }

    }


    public class SelfishPerson
    {
        public string Name { get; set; }
        public SelfishPerson MySelf { get { return this; } }
    }
}