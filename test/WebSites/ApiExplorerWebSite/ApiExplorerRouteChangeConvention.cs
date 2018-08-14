// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace ApiExplorerWebSite
{
    public class ApiExplorerRouteChangeConvention : Attribute, IActionModelConvention
    {
        public ApiExplorerRouteChangeConvention(WellKnownChangeToken changeToken)
        {
            ChangeToken = changeToken;
        }

        public WellKnownChangeToken ChangeToken { get; }

        public void Apply(ActionModel action)
        {
            if (action.Attributes.OfType<ReloadAttribute>().Any() && ChangeToken.TokenSource.IsCancellationRequested)
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
