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

using System;
using Microsoft.AspNet.Mvc;
using MvcSample.Web.Filters;
using MvcSample.Web.Models;

namespace MvcSample.Web
{
    [ServiceFilter(typeof(PassThroughAttribute), Order = 1)]
    [ServiceFilter(typeof(PassThroughAttribute))]
    [PassThrough(Order = 0)]
    [PassThrough(Order = 2)]
    [InspectResultPage]
    [BlockAnonymous]
    [TypeFilter(typeof(UserNameProvider), Order = -1)]
    public class FiltersController : Controller
    {
        public User User { get; set; }

        public FiltersController()
        {
            User = new User() { Name = "User Name", Address = "Home Address" };
        }

        // TODO: Add a real filter here
        [ServiceFilter(typeof(PassThroughAttribute))]
        [AllowAnonymous]
        [AgeEnhancer]
        [Delay(500)]
        public ActionResult Index(int age, string userName)
        {
            if (!string.IsNullOrEmpty(userName))
            {
                User.Name = userName;
            }

            User.Age = age;

            return View("MyView", User);
        }

        public ActionResult Blocked(int age, string userName)
        {
            return Index(age, userName);
        }

        [Authorize("Permission", "CanViewPage")]
        public ActionResult NotGrantedClaim(int age, string userName)
        {
            return Index(age, userName);
        }

        [FakeUser]
        [Authorize("Permission", "CanViewPage", "CanViewAnything")]
        public ActionResult AllGranted(int age, string userName)
        {
            return Index(age, userName);
        }

        [ErrorMessages, AllowAnonymous]
        public ActionResult Crash(string message)
        {
            throw new Exception(message);
        }
    }
}