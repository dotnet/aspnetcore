// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;

namespace BasicWebSite.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult PlainView()
        {
            return View();
        }

        public IActionResult ActionLinkView()
        {
            // This view contains a link generated with Html.ActionLink
            // that provides a host with non unicode characters.
            return View();
        }

        public IActionResult RedirectToActionReturningTaskAction()
        {
            return RedirectToAction("ActionReturningTask");
        }

        public IActionResult RedirectToRouteActionAsMethodAction()
        {
            return RedirectToRoute("ActionAsMethod", new { action = "ActionReturningTask", controller = "Home" });
        }

        public IActionResult NoContentResult()
        {
            return new HttpStatusCodeResult(204);
        }

        [AcceptVerbs("GET", "POST")]
        [RequireHttps]
        public IActionResult HttpsOnlyAction()
        {
            return new HttpStatusCodeResult(200);
        }

        public async Task ActionReturningTask()
        {
            // TODO: #1077. With HttpResponseMessage, there seems to be a race between the write operation setting the
            // header to 200 and HttpNoContentOutputFormatter which sets the header to 204.
            Context.Response.StatusCode = 204;
            await Context.Response.WriteAsync("Hello world");
        }

        public IActionResult JsonTextInView()
        {
            return View();
        }
    }
}