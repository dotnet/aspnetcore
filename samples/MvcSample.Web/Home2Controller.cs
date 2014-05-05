// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using MvcSample.Web.Models;

namespace MvcSample.Web.RandomNameSpace
{
    public class Home2Controller
    {
        private User _user = new User() { Name = "User Name", Address = "Home Address" };

        public Home2Controller(IActionResultHelper actionResultHelper)
        {
            Result = actionResultHelper;
        }

        public IActionResultHelper Result { get; private set; }

        public HttpContext Context
        {
            get
            {
                return ActionContext.HttpContext;
            }
        }

        public ActionContext ActionContext { get; set; }

        public string Index()
        {
            return "Hello World: my namespace is " + this.GetType().Namespace;
        }

        public ActionResult Something()
        {
            return new ContentResult
            {
                Content = "Hello World From Content"
            };
        }

        public ActionResult Hello()
        {
            return Result.Content("Hello World", null, null);
        }

        public void Raw()
        {
            Context.Response.WriteAsync("Hello World raw");
        }

        public ActionResult UserJson()
        {
            var jsonResult = Result.Json(_user);
            jsonResult.Indent = false;

            return jsonResult;
        }

        public User User()
        {
            return _user;
        }
    }
}