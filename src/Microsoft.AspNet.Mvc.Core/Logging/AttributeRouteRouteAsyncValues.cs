// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using Microsoft.AspNet.Routing.Template;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Represents the state of <see cref="Microsoft.AspNet.Mvc.Routing.AttributeRoute.RouteAsync(
    /// AspNet.Routing.RouteContext)"/>.
    /// </summary>
    public class AttributeRouteRouteAsyncValues
    {
        /// <summary>
        /// The name of the state.
        /// </summary>
        public string Name
        {
            get
            {
                return "AttributeRoute.RouteAsync";
            }
        }

        /// <summary>
        /// The matching routes.
        /// </summary>
        public IList<TemplateRoute> MatchingRoutes { get; set; }

        /// <summary>
        /// True if the request is handled.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// A summary of the data for display.
        /// </summary>
        public string Summary
        {
            get
            {
                var builder = new StringBuilder();
                builder.AppendLine(Name);
                builder.Append("\tMatching routes: ");
                StringBuilderHelpers.Append(builder, MatchingRoutes);
                builder.AppendLine();
                builder.Append("\tHandled? ");
                builder.Append(Handled);
                return builder.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Summary;
        }
    }
}