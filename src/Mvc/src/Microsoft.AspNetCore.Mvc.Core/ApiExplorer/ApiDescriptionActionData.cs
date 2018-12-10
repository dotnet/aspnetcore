// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Represents data used to build an <c>ApiDescription</c>, stored as part of the
    /// <see cref="Abstractions.ActionDescriptor.Properties"/>.
    /// </summary>
    internal class ApiDescriptionActionData
    {
        public static ApiDescriptionActionData Create(
            ApplicationModel application,
            ControllerModel controller,
            ActionModel action,
            SelectorModel selector)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            var isVisible =
                action.ApiExplorer?.IsVisible ??
                controller.ApiExplorer?.IsVisible ??
                application.ApiExplorer?.IsVisible ??
                false;

            var isVisibleSetOnActionOrController =
                action.ApiExplorer?.IsVisible ??
                controller.ApiExplorer?.IsVisible ??
                false;

            // ApiExplorer isn't supported on conventional-routed actions, but we still allow you to configure
            // it at the application level when you have a mix of controller types. We'll just skip over enabling
            // ApiExplorer for conventional-routed controllers when this happens.
            var isVisibleSetOnApplication = application.ApiExplorer?.IsVisible ?? false;

            if (isVisibleSetOnActionOrController && !IsAttributeRouted())
            {
                // ApiExplorer is only supported on attribute routed actions.
                throw new InvalidOperationException(Resources.FormatApiExplorer_UnsupportedAction(
                    ControllerActionDescriptor.GetDefaultDisplayName(
                        controller.ControllerType,
                        action.ActionMethod)));
            }
            else if (isVisibleSetOnApplication && !IsAttributeRouted())
            {
                // This is the case where we're going to be lenient, just ignore it.
            }
            else if (isVisible)
            {
                Debug.Assert(IsAttributeRouted());

                return new ApiDescriptionActionData()
                {
                    GroupName = action.ApiExplorer?.GroupName ?? controller.ApiExplorer?.GroupName,
                };
            }

            return null;

            bool IsAttributeRouted()
            {
                return selector.AttributeRouteModel != null;
            }
        }

        /// <summary>
        /// The <c>ApiDescription.GroupName</c> of <c>ApiDescription</c> objects for the associated
        /// action.
        /// </summary>
        public string GroupName { get; set; }
    }
}
