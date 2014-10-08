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
        /// The list of actions that matched all their route constraints, and action constraints, if any.
        /// </summary>
        public IReadOnlyList<ActionDescriptor> ActionsMatchingActionConstraints { get; set; }

        /// <summary>
        /// The list of actions that are the best matches. These match all constraints and any additional criteria
        /// for disambiguation.
        /// </summary>
        public IReadOnlyList<ActionDescriptor> FinalMatches { get; set; }

        /// <summary>
        /// The selected action. Will be null if no matches are found or more than one match is found.
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
                builder.Append("\tActions matching action constraints: ");
                StringBuilderHelpers.Append(builder, ActionsMatchingActionConstraints, Formatter);
                builder.AppendLine();
                builder.Append("\tFinal Matches: ");
                StringBuilderHelpers.Append(builder, FinalMatches, Formatter);
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
            return descriptor != null ? descriptor.DisplayName : "No action selected";
        }
    }
}