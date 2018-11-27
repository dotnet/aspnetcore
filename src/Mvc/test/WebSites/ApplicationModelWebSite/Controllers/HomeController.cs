// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApplicationModelWebSite
{
    public class HomeController : Controller
    {
        public string GetCommonDescription()
        {
            return ControllerContext.ActionDescriptor.Properties["description"].ToString();
        }

        [HttpGet("Home/GetHelloWorld")]
        public object GetHelloWorld([FromHeader] string helloWorld)
        {
            return ControllerContext.ActionDescriptor.Properties["source"].ToString() + " - " + helloWorld;
        }

        [HttpGet("Home/CannotBeRouted", Name = nameof(SuppressPathMatching))]
        [HttpGet("Home/CanBeRouted")]
        [SuppressPatchMatchingConvention]
        public object SuppressPathMatching()
        {
            return "Hello world";
        }

        [HttpGet("Home/SuppressLinkGeneration", Name = nameof(SuppressLinkGeneration))]
        [SuppressLinkGenerationConvention]
        public object SuppressLinkGeneration() => "Hello world";

        [HttpGet("Home/RouteToSuppressLinkGeneration")]
        public IActionResult RouteToSuppressLinkGeneration() => RedirectToRoute(nameof(SuppressLinkGeneration));

        [HttpGet("Home/RouteToSuppressPathMatching")]
        public IActionResult RouteToSuppressPathMatching() => RedirectToRoute(nameof(SuppressPathMatching));

        private class SuppressPatchMatchingConvention : Attribute, IActionModelConvention
        {
            public void Apply(ActionModel model)
            {
                var selector = model.Selectors.First(f => f.AttributeRouteModel.Template == "Home/CannotBeRouted");
                selector.AttributeRouteModel.SuppressPathMatching = true;
            }
        }

        private class SuppressLinkGenerationConvention : Attribute, IActionModelConvention
        {
            public void Apply(ActionModel model)
            {
                model.Selectors[0].AttributeRouteModel.SuppressLinkGeneration = true;
            }
        }
    }
}