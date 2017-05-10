// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service.Mvc
{
    public static class IdentityServiceControllerExtensions
    {
        public static IActionResult FormPost(this ControllerBase controller, AuthorizationRequestError error)
        {
            return new FormPostResult(error.RedirectUri, error.Message.Parameters);
        }

        public static IActionResult Fragment(this ControllerBase controller, AuthorizationRequestError error)
        {
            return new FragmentResult(error.RedirectUri, error.Message.Parameters);
        }

        public static IActionResult Query(this ControllerBase controller, AuthorizationRequestError error)
        {
            return new QueryResult(error.RedirectUri, error.Message.Parameters);
        }

        public static IActionResult FormPost(this ControllerBase controller, AuthorizationResponse response)
        {
            return new FormPostResult(response.RedirectUri, response.Message.Parameters);
        }

        public static IActionResult Fragment(this ControllerBase controller, AuthorizationResponse response)
        {
            return new FragmentResult(response.RedirectUri, response.Message.Parameters);
        }

        public static IActionResult Query(this ControllerBase controller, AuthorizationResponse response)
        {
            return new QueryResult(response.RedirectUri, response.Message.Parameters);
        }

        public static IActionResult InvalidAuthorization(this ControllerBase controller, AuthorizationRequestError error)
        {
            switch (error.ResponseMode)
            {
                case OpenIdConnectResponseMode.FormPost:
                    return controller.FormPost(error);
                case OpenIdConnectResponseMode.Fragment:
                    return controller.Fragment(error);
                case OpenIdConnectResponseMode.Query:
                    return controller.Query(error);
                default:
                    return new BadRequestResult();
            }
        }

        public static IActionResult ValidAuthorization(this ControllerBase controller, AuthorizationResponse response)
        {
            switch (response.ResponseMode)
            {
                case OpenIdConnectResponseMode.FormPost:
                    return controller.FormPost(response);
                case OpenIdConnectResponseMode.Fragment:
                    return controller.Fragment(response);
                case OpenIdConnectResponseMode.Query:
                    return controller.Query(response);
                default:
                    throw new InvalidOperationException("Invalid response mode.");
            }
        }
    }
}
