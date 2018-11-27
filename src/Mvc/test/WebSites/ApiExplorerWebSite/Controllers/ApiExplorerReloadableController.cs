// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApiExplorerWebSite
{
    [Route("ApiExplorerReload")]
    public class ApiExplorerReloadableController : Controller
    {
        [ApiExplorerRouteChangeConvention]
        [Route("Index")]
        public string Index() => "Hello world";

        [Route("Reload")]
        [PassThru]
        public IActionResult Reload()
        {
            ActionDescriptorChangeProvider.Instance.HasChanged = true;
            ActionDescriptorChangeProvider.Instance.TokenSource.Cancel();
            return Ok();
        }

        public class ApiExplorerRouteChangeConventionAttribute : Attribute, IActionModelConvention
        {
            public void Apply(ActionModel action)
            {
                if (ActionDescriptorChangeProvider.Instance.HasChanged)
                {
                    action.ActionName = "NewIndex";
                    action.Selectors.Clear();
                    action.Selectors.Add(new SelectorModel
                    {
                        AttributeRouteModel = new AttributeRouteModel
                        {
                            Template = "NewIndex"
                        }
                    });
                }
            }
        }
    }
}
