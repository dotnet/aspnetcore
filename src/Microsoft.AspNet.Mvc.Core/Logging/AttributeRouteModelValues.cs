// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Logging representation of the state of a <see cref="AttributeRouteModel"/>. Logged as a substructure of
    /// <see cref="ControllerModelValues"/>.
    /// </summary>
    public class AttributeRouteModelValues : LoggerStructureBase
    {
        public AttributeRouteModelValues(AttributeRouteModel inner)
        {
            if (inner != null)
            {
                Template = inner.Template;
                Order = inner.Order;
                Name = inner.Name;
                IsAbsoluteTemplate = inner.IsAbsoluteTemplate;
            }
        }

        /// <summary>
        /// The template of the route. See <see cref="AttributeRouteModel.Template"/>.
        /// </summary>
        public string Template { get; }

        /// <summary>
        /// The order of the route. See <see cref="AttributeRouteModel.Order"/>.
        /// </summary>
        public int? Order { get; }

        /// <summary>
        /// The name of the route. See <see cref="AttributeRouteModel.Name"/>.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Whether or not the template is absolute. See <see cref="AttributeRouteModel.IsAbsoluteTemplate"/>.
        /// </summary>
        public bool IsAbsoluteTemplate { get; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}