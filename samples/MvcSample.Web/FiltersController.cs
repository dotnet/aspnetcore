// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        public User CustomUser { get; set; }

        public FiltersController()
        {
            CustomUser = new User() { Name = "User Name", Address = "Home Address" };
        }

        [ServiceFilter(typeof(PassThroughAttribute))]
        [AllowAnonymous]
        [AgeEnhancerFilter]
        [Delay(500)]
        public ActionResult Index(int age = 20, string userName = "SampleUser")
        {
            if (!string.IsNullOrEmpty(userName))
            {
                CustomUser.Name = userName;
            }

            CustomUser.Age = age;

            return View("MyView", CustomUser);
        }

        public ActionResult Blocked(int age = 20, string userName = "SampleUser")
        {
            return Index(age, userName);
        }

        public ActionResult ChallengeUser(int age = 20, string userName = "SampleUser")
        {
            return new ChallengeResult();
        }

        [Authorize("CanViewPage")]
        public ActionResult NotGrantedClaim(int age = 20, string userName = "SampleUser")
        {
            return Index(age, userName);
        }

        [FakeUser]
        [Authorize("CanViewAnything")]
        public ActionResult AllGranted(int age = 20, string userName = "SampleUser")
        {
            return Index(age, userName);
        }

        [ErrorMessages, AllowAnonymous]
        public ActionResult Crash(string message = "Sample crash message")
        {
            throw new Exception(message);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.DidTheFilterRun = "Totally!";
        }
    }
}