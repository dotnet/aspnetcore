// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNet.Mvc;

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
    }

    public class SelfishPerson
    {
        public string Name { get; set; }
        public SelfishPerson MySelf { get { return this; } }
    }
}