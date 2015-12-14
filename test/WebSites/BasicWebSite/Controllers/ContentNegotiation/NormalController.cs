// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicWebSite.Formatters;
using BasicWebSite.Models;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Formatters;
using Newtonsoft.Json;

namespace BasicWebSite.Controllers.ContentNegotiation
{
    public class NormalController : Controller
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var result = context.Result as ObjectResult;
            if (result != null)
            {
                result.Formatters.Add(new PlainTextFormatter());
                result.Formatters.Add(new CustomFormatter("application/custom"));

                var jsonFormatter = new JsonOutputFormatter();
                jsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
                result.Formatters.Add(jsonFormatter);
            }

            base.OnActionExecuted(context);
        }

        public string ReturnClassName()
        {
            return "NormalController";
        }

        public User ReturnUser()
        {
            return CreateUser();
        }

        [Produces("application/NoFormatter")]
        public User ReturnUser_NoMatchingFormatter()
        {
            return CreateUser();
        }

        [Produces("application/custom", "application/json", "text/json")]
        public User MultipleAllowedContentTypes()
        {
            return CreateUser();
        }

        [Produces("application/custom")]
        public string WriteUserUsingCustomFormat()
        {
            return "Written using custom format.";
        }

        [NonAction]
        public User CreateUser()
        {
            User user = new User()
            {
                Name = "My name",
                Address = "My address",
            };

            return user;
        }
    }
}