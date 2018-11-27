// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;

namespace WebApiCompatShimWebSite
{
    public class ActionResultController : ApiController
    {
        private static readonly JsonSerializerSettings _indentedSettings;

        static ActionResultController()
        {
            _indentedSettings = JsonSerializerSettingsProvider.CreateSerializerSettings();
            _indentedSettings.Formatting = Formatting.Indented;
        }

        public IActionResult GetBadRequest()
        {
            return BadRequest();
        }

        public IActionResult GetBadRequestMessage()
        {
            return BadRequest("Hello, world!");
        }

        public IActionResult GetBadRequestModelState()
        {
            ModelState.AddModelError("product.Name", "Name is required.");
            return BadRequest(ModelState);
        }

        public IActionResult GetConflict()
        {
            return Conflict();
        }

        public IActionResult GetContent()
        {
            return Content(HttpStatusCode.Ambiguous, CreateUser());
        }

        public IActionResult GetCreatedRelative()
        {
            return Created("5", CreateUser());
        }

        public IActionResult GetCreatedAbsolute()
        {
            return Created("/api/Blog/ActionResult/GetUser/5", CreateUser());
        }

        public IActionResult GetCreatedQualified()
        {
            return Created("http://localhost/api/Blog/ActionResult/5", CreateUser());
        }

        public IActionResult GetCreatedUri()
        {
            return Created(new Uri("/api/Blog/ActionResult/GetUser/5", UriKind.Relative), CreateUser());
        }

        public IActionResult GetCreatedAtRoute()
        {
            var values = new { controller = "ActionResult", action = "GetUser", id = 5 };
            return CreatedAtRoute("named-action", values, CreateUser());
        }

        public IActionResult GetInternalServerError()
        {
            return InternalServerError();
        }

        public IActionResult GetInternalServerErrorException()
        {
            return InternalServerError(new Exception("Error not passed to client."));
        }

        public IActionResult GetJson()
        {
            return Json(CreateUser());
        }

        public IActionResult GetJsonSettings()
        {
            return Json(CreateUser(), _indentedSettings);
        }

        public IActionResult GetJsonSettingsEncoding()
        {
            return Json(CreateUser(), _indentedSettings, Encoding.UTF32);
        }

        public IActionResult GetNotFound()
        {
            return NotFound();
        }

        public IActionResult GetOk()
        {
            return Ok();
        }

        public IActionResult GetOkContent()
        {
            return Ok(CreateUser());
        }

        public IActionResult GetRedirectString()
        {
            // strings must be absolute URIs
            return Redirect("http://localhost/api/Users");
        }

        public IActionResult GetRedirectUri()
        {
            // Uris can be absolute or relative
            return Redirect(new Uri("api/Blog", UriKind.RelativeOrAbsolute));
        }

        public IActionResult GetRedirectUrlUsingRouteName()
        {
            return RedirectToRoute("named-action", new { controller = "BasicApi", action = "WriteToHttpContext" });
        }

        public IActionResult GetResponseMessage()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Add("X-Test", "Hello");

            return ResponseMessage(response);
        }

        public IActionResult GetStatusCode()
        {
            return StatusCode(HttpStatusCode.PaymentRequired);
        }

        // Used for generating links
        public User GetUser(int id)
        {
            return CreateUser();
        }

        private User CreateUser()
        {
            return new User()
            {
                Name = "Test User",
            };
        }
    }
}