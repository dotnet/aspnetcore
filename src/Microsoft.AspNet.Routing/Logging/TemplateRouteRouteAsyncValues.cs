// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNet.Routing.Logging
{
    /// <summary>
    /// Describes the state of
    /// <see cref="Microsoft.AspNet.Routing.Template.TemplateRoute.RouteAsync(RouteContext)"/>.
    /// </summary>
    public class TemplateRouteRouteAsyncValues
    {
        /// <summary>
        /// The name of the state.
        /// </summary>
        public string Name
        {
            get
            {
                return "TemplateRoute.RouteAsync";
            }
        }

        /// <summary>
        /// The target.
        /// </summary>
        public IRouter Target { get; set; }

        /// <summary>
        /// The template.
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// The request path.
        /// </summary>
        public string RequestPath { get; set; }

        /// <summary>
        /// The values produced by default.
        /// </summary>
        public IReadOnlyDictionary<string, object> DefaultValues { get; set; }

        /// <summary>
        /// The values produced from the request.
        /// </summary>
        public IDictionary<string, object> ProducedValues { get; set; }

        /// <summary>
        /// The constraints matched on the produced values.
        /// </summary>
        public IReadOnlyDictionary<string, IRouteConstraint> Constraints { get; set; }

        /// <summary>
        /// True if the <see cref="ProducedValues"/> matched.
        /// </summary>
        public bool MatchedTemplate { get; set; }

        /// <summary>
        /// True if the <see cref="Constraints"/> matched.
        /// </summary>
        public bool MatchedConstraints { get; set; }

        /// <summary>
        /// True if this route matched.
        /// </summary>
        public bool Matched { get; set; }

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
                builder.Append("\tTarget: ");
                builder.Append(Target);
                builder.AppendLine();
                builder.Append("\tTemplate: ");
                builder.AppendLine(Template);
                builder.Append("\tRequest path: ");
                builder.AppendLine(RequestPath);
                builder.Append("\tDefault values: ");
                StringBuilderHelpers.Append(builder, DefaultValues);
                builder.AppendLine();
                builder.Append("\tProduced values: ");
                StringBuilderHelpers.Append(builder, ProducedValues);
                builder.AppendLine();
                builder.Append("\tConstraints: ");
                StringBuilderHelpers.Append(builder, Constraints);
                builder.AppendLine();
                builder.Append("\tMatched template? ");
                builder.Append(MatchedTemplate);
                builder.AppendLine();
                builder.Append("\tMatched constraints? ");
                builder.Append(MatchedConstraints);
                builder.AppendLine();
                builder.Append("\tMatched? ");
                builder.Append(Matched);
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