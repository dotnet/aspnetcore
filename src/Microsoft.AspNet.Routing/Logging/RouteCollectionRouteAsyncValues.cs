// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNet.Routing.Logging
{
    /// <summary>
    /// Describes the state of
    /// <see cref="Microsoft.AspNet.Routing.RouteCollection.RouteAsync(RouteContext)"/>.
    /// </summary>
    public class RouteCollectionRouteAsyncValues
    {
        /// <summary>
        /// The name of the state.
        /// </summary>
        public string Name
        {
            get
            {
                return "RouteCollection.RouteAsync";
            }
        }

        /// <summary>
        /// The available routes.
        /// </summary>
        public IList<IRouter> Routes { get; set; }

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
                builder.Append("\tRoutes: ");
                StringBuilderHelpers.Append(builder, Routes);
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