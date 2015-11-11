// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
            if (byteValues == null)
            {
                return Content(content: null);
            }

            return Content(System.Text.Encoding.UTF8.GetString(byteValues));
        }

        [HttpPost("/integers")]
        public IActionResult CollectionToJson(int[] model)
        {
            return Json(model);
        }

        [HttpPost("/cities")]
        public IActionResult PocoCollectionToJson(List<City> model)
        {
            return Json(model);
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
            return token == HttpContext.RequestAborted;
        }

        public bool ActionWithCancellationTokenModel(CancellationTokenModel wrapper)
        {
            return wrapper.CancellationToken == HttpContext.RequestAborted;
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

        public Customer GetCustomer(int id)
        {
            var customer = CreateCustomer(id);

            // should update customer.Department from body.
            TryUpdateModelAsync(customer);

            return customer;
        }

        public IActionResult GetErrorMessage(DateTime birthdate)
        {
            return Content(ModelState[nameof(birthdate)].Errors[0].ErrorMessage);
        }

        private Customer CreateCustomer(int id)
        {
            return new Customer()
            {
                Id = id,
                Name = "dummy",
                Age = 25,
            };
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