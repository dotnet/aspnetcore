// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Represents the state of <see cref="DefaultActionSelector.SelectAsync(AspNet.Routing.RouteContext)"/>.
    /// </summary>
    public class DefaultActionSelectorSelectAsyncValues
    {
        /// <summary>
        /// The name of the state.
        /// </summary>
        public string Name
        {
            get
            {
                return "DefaultActionSelector.SelectAsync";
            }
        }

        /// <summary>
        /// The list of actions that matched all their route constraints, if any.
        /// </summary>
        public IReadOnlyList<ActionDescriptor> ActionsMatchingRouteConstraints { get; set; }

        /// <summary>
        /// The list of actions that matched all their route and method constraints, if any.
        /// </summary>
        public IReadOnlyList<ActionDescriptor> ActionsMatchingRouteAndMethodConstraints { get; set; }

        /// <summary>
        /// The list of actions that matched all their route, method, and dynamic constraints, if any.
        /// </summary>
        public IReadOnlyList<ActionDescriptor> ActionsMatchingRouteAndMethodAndDynamicConstraints { get; set; }

        /// <summary>
        /// The actions that matched with at least one constraint.
        /// </summary>
        public IReadOnlyList<ActionDescriptor> ActionsMatchingWithConstraints { get; set; }

        /// <summary>
        /// The selected action.
        /// </summary>
        public ActionDescriptor SelectedAction { get; set; }

        /// <summary>
        /// A summary of the data for display.
        /// </summary>
        public string Summary
        {
            get
            {
                var builder = new StringBuilder();
                builder.AppendLine(Name);
                builder.Append("\tActions matching route constraints: ");
                StringBuilderHelpers.Append(builder, ActionsMatchingRouteConstraints, Formatter);
                builder.AppendLine();
                builder.Append("\tActions matching route and method constraints: ");
                StringBuilderHelpers.Append(builder, ActionsMatchingRouteAndMethodConstraints, Formatter);
                builder.AppendLine();
                builder.Append("\tActions matching route, method, and dynamic constraints: ");
                StringBuilderHelpers.Append(builder, ActionsMatchingRouteAndMethodAndDynamicConstraints, Formatter);
                builder.AppendLine();
                builder.Append("\tActions matching with at least one constraint: ");
                StringBuilderHelpers.Append(builder, ActionsMatchingWithConstraints, Formatter);
                builder.AppendLine();
                builder.Append("\tSelected action: ");
                builder.Append(Formatter(SelectedAction));
                return builder.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Summary;
        }

        private string Formatter(ActionDescriptor descriptor)
        {
            return descriptor.DisplayName;
        }
    }
}