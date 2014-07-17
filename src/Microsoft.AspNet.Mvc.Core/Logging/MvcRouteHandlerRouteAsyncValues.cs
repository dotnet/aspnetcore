// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Represents the state of <see cref="MvcRouteHandler.RouteAsync(AspNet.Routing.RouteContext)"/>.
    /// </summary>
    public class MvcRouteHandlerRouteAsyncValues
    {
        /// <summary>
        /// The name of the state.
        /// </summary>
        public string Name
        {
            get
            {
                return "MvcRouteHandler.RouteAsync";
            }
        }

        /// <summary>
        /// True if an action was selected.
        /// </summary>
        public bool ActionSelected { get; set; }

        /// <summary>
        /// True if the selected action was invoked.
        /// </summary>
        public bool ActionInvoked { get; set; }

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
                builder.Append("\tAction selected? ");
                builder.Append(ActionSelected);
                builder.AppendLine();
                builder.Append("\tAction invoked? ");
                builder.Append(ActionInvoked);
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