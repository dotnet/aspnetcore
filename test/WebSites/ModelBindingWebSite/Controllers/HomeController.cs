// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Mvc;
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

        public bool ActionWithCancellationToken(CancellationToken token)
        {
            return token == ActionContext.HttpContext.RequestAborted;
        }

        public bool ActionWithCancellationTokenModel(CancellationTokenModel wrapper)
        {
            return wrapper.CancellationToken == ActionContext.HttpContext.RequestAborted;
        }

        [HttpGet("Home/ActionWithPersonFromUrlWithPrefix/{person.name}/{person.age}")]
        public Person ActionWithPersonFromUrlWithPrefix([FromRoute] Person person)
        {
            return person;
        }

        [HttpGet("Home/ActionWithPersonFromUrlWithoutPrefix/{name}/{age}")]
        public Person ActionWithPersonFromUrlWithoutPrefix([FromRoute] Person person)
        {
            return person;
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

        public class CancellationTokenModel
        {
            public CancellationToken CancellationToken { get; set; }
        }
    }
}