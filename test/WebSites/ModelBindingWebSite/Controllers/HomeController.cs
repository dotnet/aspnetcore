// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc;
using System.Linq;
using ModelBindingWebSite.Models;

namespace ModelBindingWebSite.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index(byte[] byteValues)
        {
            return Content(System.Text.Encoding.UTF8.GetString(byteValues));
        }

        public object ModelWithTooManyValidationErrors(LargeModelWithValidation model)
        {
            return CreateValidationDictionary();
        }

        public object ModelWithFewValidationErrors(ModelWithValidation model)
        {
            return CreateValidationDictionary();
        }

        private Dictionary<string, string> CreateValidationDictionary()
        {
            var result = new Dictionary<string, string>();
            foreach (var item in ModelState)
            {
                var error = item.Value.Errors.SingleOrDefault();
                if (error != null)
                {
                    var value = error.Exception != null ? error.Exception.Message :
                                                          error.ErrorMessage;
                    result.Add(item.Key, value);
                }
            }

            return result;
        }
    }
}