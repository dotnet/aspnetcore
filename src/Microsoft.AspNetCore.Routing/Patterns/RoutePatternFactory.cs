// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing.Patterns
{
    /// <summary>
    /// Contains factory methods for creating <see cref="RoutePattern"/> and related types.
    /// Use <see cref="Parse(string)"/> to parse a route pattern in
    /// string format.
    /// </summary>
    public static class RoutePatternFactory
    {
        /// <summary>
        /// Creates a <see cref="RoutePattern"/> from its string representation.
        /// </summary>
        /// <param name="pattern">The route pattern string to parse.</param>
        /// <returns>The <see cref="RoutePattern"/>.</returns>
        public static RoutePattern Parse(string pattern)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            return RoutePatternParser.Parse(pattern);
        }

        /// <summary>
        /// Creates a <see cref="RoutePattern"/> from its string representation along
        /// with provided default values and constraints.
        /// </summary>
        /// <param name="pattern">The route pattern string to parse.</param>
        /// <param name="defaults">
        /// Additional default values to associated with the route pattern. May be null.
        /// The provided object will be converted to key-value pairs using <see cref="RouteValueDictionary"/>
        /// and then merged into the parsed route pattern.
        /// </param>
        /// <param name="constraints">
        /// Additional constraints to associated with the route pattern. May be null.
        /// The provided object will be converted to key-value pairs using <see cref="RouteValueDictionary"/>
        /// and then merged into the parsed route pattern.
        /// </param>
        /// <returns>The <see cref="RoutePattern"/>.</returns>
        public static RoutePattern Parse(string pattern, object defaults, object constraints)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            var original = RoutePatternParser.Parse(pattern);
            return Pattern(original.RawText, defaults, constraints, original.PathSegments);
        }

        /// <summary>
        /// Creates a new <see cref="RoutePattern"/> from a collection of segments.
        /// </summary>
        /// <param name="segments">The collection of segments.</param>
        /// <returns>The <see cref="RoutePattern"/>.</returns>
        public static RoutePattern Pattern(IEnumerable<RoutePatternPathSegment> segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            return PatternCore(null, null, null, segments);
        }

        /// <summary>
        /// Creates a new <see cref="RoutePattern"/> from a collection of segments.
        /// </summary>
        /// <param name="rawText">The raw text to associate with the route pattern. May be null.</param>
        /// <param name="segments">The collection of segments.</param>
        /// <returns>The <see cref="RoutePattern"/>.</returns>
        public static RoutePattern Pattern(string rawText, IEnumerable<RoutePatternPathSegment> segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            return PatternCore(rawText, null, null, segments);
        }

        /// <summary>
        /// Creates a <see cref="RoutePattern"/> from a collection of segments along
        /// with provided default values and constraints.
        /// </summary>
        /// <param name="defaults">
        /// Additional default values to associated with the route pattern. May be null.
        /// The provided object will be converted to key-value pairs using <see cref="RouteValueDictionary"/>
        /// and then merged into the route pattern.
        /// </param>
        /// <param name="constraints">
        /// Additional constraints to associated with the route pattern. May be null.
        /// The provided object will be converted to key-value pairs using <see cref="RouteValueDictionary"/>
        /// and then merged into the route pattern.
        /// </param>
        /// <param name="segments">The collection of segments.</param>
        /// <returns>The <see cref="RoutePattern"/>.</returns>
        public static RoutePattern Pattern(
            object defaults,
            object constraints,
            IEnumerable<RoutePatternPathSegment> segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            return PatternCore(null, new RouteValueDictionary(defaults), new RouteValueDictionary(constraints), segments);
        }

        /// <summary>
        /// Creates a <see cref="RoutePattern"/> from a collection of segments along
        /// with provided default values and constraints.
        /// </summary>
        /// <param name="rawText">The raw text to associate with the route pattern.</param>
        /// <param name="defaults">
        /// Additional default values to associated with the route pattern. May be null.
        /// The provided object will be converted to key-value pairs using <see cref="RouteValueDictionary"/>
        /// and then merged into the route pattern.
        /// </param>
        /// <param name="constraints">
        /// Additional constraints to associated with the route pattern. May be null.
        /// The provided object will be converted to key-value pairs using <see cref="RouteValueDictionary"/>
        /// and then merged into the route pattern.
        /// </param>
        /// <param name="segments">The collection of segments.</param>
        /// <returns>The <see cref="RoutePattern"/>.</returns>
        public static RoutePattern Pattern(
            string rawText,
            object defaults,
            object constraints,
            IEnumerable<RoutePatternPathSegment> segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            return PatternCore(rawText, new RouteValueDictionary(defaults), new RouteValueDictionary(constraints), segments);
        }

        /// <summary>
        /// Creates a new <see cref="RoutePattern"/> from a collection of segments.
        /// </summary>
        /// <param name="segments">The collection of segments.</param>
        /// <returns>The <see cref="RoutePattern"/>.</returns>
        public static RoutePattern Pattern(params RoutePatternPathSegment[] segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            return PatternCore(null, null, null, segments);
        }

        /// <summary>
        /// Creates a new <see cref="RoutePattern"/> from a collection of segments.
        /// </summary>
        /// <param name="rawText">The raw text to associate with the route pattern. May be null.</param>
        /// <param name="segments">The collection of segments.</param>
        /// <returns>The <see cref="RoutePattern"/>.</returns>
        public static RoutePattern Pattern(string rawText, params RoutePatternPathSegment[] segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            return PatternCore(rawText, null, null, segments);
        }

        /// <summary>
        /// Creates a <see cref="RoutePattern"/> from a collection of segments along
        /// with provided default values and constraints.
        /// </summary>
        /// <param name="defaults">
        /// Additional default values to associated with the route pattern. May be null.
        /// The provided object will be converted to key-value pairs using <see cref="RouteValueDictionary"/>
        /// and then merged into the route pattern.
        /// </param>
        /// <param name="constraints">
        /// Additional constraints to associated with the route pattern. May be null.
        /// The provided object will be converted to key-value pairs using <see cref="RouteValueDictionary"/>
        /// and then merged into the route pattern.
        /// </param>
        /// <param name="segments">The collection of segments.</param>
        /// <returns>The <see cref="RoutePattern"/>.</returns>
        public static RoutePattern Pattern(
            object defaults,
            object constraints,
            params RoutePatternPathSegment[] segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            return PatternCore(null, new RouteValueDictionary(defaults), new RouteValueDictionary(constraints), segments);
        }

        /// <summary>
        /// Creates a <see cref="RoutePattern"/> from a collection of segments along
        /// with provided default values and constraints.
        /// </summary>
        /// <param name="rawText">The raw text to associate with the route pattern.</param>
        /// <param name="defaults">
        /// Additional default values to associated with the route pattern. May be null.
        /// The provided object will be converted to key-value pairs using <see cref="RouteValueDictionary"/>
        /// and then merged into the route pattern.
        /// </param>
        /// <param name="constraints">
        /// Additional constraints to associated with the route pattern. May be null.
        /// The provided object will be converted to key-value pairs using <see cref="RouteValueDictionary"/>
        /// and then merged into the route pattern.
        /// </param>
        /// <param name="segments">The collection of segments.</param>
        /// <returns>The <see cref="RoutePattern"/>.</returns>
        public static RoutePattern Pattern(
            string rawText,
            object defaults,
            object constraints,
            params RoutePatternPathSegment[] segments)
        {
            if (segments == null)
            {
                throw new ArgumentNullException(nameof(segments));
            }

            return PatternCore(rawText, new RouteValueDictionary(defaults), new RouteValueDictionary(constraints), segments);
        }

        private static RoutePattern PatternCore(
            string rawText,
            IDictionary<string, object> defaults,
            IDictionary<string, object> constraints,
            IEnumerable<RoutePatternPathSegment> segments)
        {
            // We want to merge the segment data with the 'out of line' defaults and constraints.
            //
            // This means that for parameters that have 'out of line' defaults we will modify
            // the parameter to contain the default (same story for constraints).
            //
            // We also maintain a collection of defaults and constraints that will also
            // contain the values that don't match a parameter.
            //
            // It's important that these two views of the data are consistent. We don't want
            // values specified out of line to have a different behavior.

            var updatedDefaults = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (defaults != null)
            {
                foreach (var kvp in defaults)
                {
                    updatedDefaults.Add(kvp.Key, kvp.Value);
                }
            }

            var updatedConstraints = new Dictionary<string, List<RoutePatternConstraintReference>>(StringComparer.OrdinalIgnoreCase);
            if (constraints != null)
            {
                foreach (var kvp in constraints)
                {
                    updatedConstraints.Add(kvp.Key, new List<RoutePatternConstraintReference>()
                    {
                        Constraint(kvp.Value),
                    });
                }
            }

            var parameters = new List<RoutePatternParameterPart>();
            var updatedSegments = segments.ToArray();
            for (var i = 0; i < updatedSegments.Length; i++)
            {
                var segment = VisitSegment(updatedSegments[i]);
                updatedSegments[i] = segment;
                
                for (var j = 0; j < segment.Parts.Count; j++)
                {
                    if (segment.Parts[j] is RoutePatternParameterPart parameter)
                    {
                        parameters.Add(parameter);
                    }
                }
            }

            return new RoutePattern(
                rawText,
                updatedDefaults,
                updatedConstraints.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<RoutePatternConstraintReference>)kvp.Value.ToArray()),
                parameters.ToArray(),
                updatedSegments.ToArray());

            RoutePatternPathSegment VisitSegment(RoutePatternPathSegment segment)
            {
                var updatedParts = new RoutePatternPart[segment.Parts.Count];
                for (var i = 0; i < segment.Parts.Count; i++)
                {
                    var part = segment.Parts[i];
                    updatedParts[i] = VisitPart(part);
                }

                return SegmentCore(updatedParts);
            }

            RoutePatternPart VisitPart(RoutePatternPart part)
            {
                if (!part.IsParameter)
                {
                    return part;
                }

                var parameter = (RoutePatternParameterPart)part;
                var @default = parameter.Default;

                if (updatedDefaults.TryGetValue(parameter.Name, out var newDefault))
                {
                    if (parameter.Default != null)
                    {
                        var message = Resources.FormatTemplateRoute_CannotHaveDefaultValueSpecifiedInlineAndExplicitly(parameter.Name);
                        throw new InvalidOperationException(message);
                    }

                    if (parameter.IsOptional)
                    {
                        var message = Resources.TemplateRoute_OptionalCannotHaveDefaultValue;
                        throw new InvalidOperationException(message);
                    }

                    @default = newDefault;
                }
                
                if (parameter.Default != null)
                {
                    updatedDefaults.Add(parameter.Name, parameter.Default);
                }

                if (!updatedConstraints.TryGetValue(parameter.Name, out var parameterConstraints) &&
                    parameter.Constraints.Count > 0)
                {
                    parameterConstraints = new List<RoutePatternConstraintReference>();
                    updatedConstraints.Add(parameter.Name, parameterConstraints);
                }

                if (parameter.Constraints.Count > 0)
                {
                    parameterConstraints.AddRange(parameter.Constraints);
                }

                return ParameterPartCore(
                    parameter.Name,
                    @default,
                    parameter.ParameterKind,
                    (IEnumerable<RoutePatternConstraintReference>)parameterConstraints ?? Array.Empty<RoutePatternConstraintReference>());
            }
        }

        /// <summary>
        /// Creates a <see cref="RoutePatternPathSegment"/> from the provided collection
        /// of parts.
        /// </summary>
        /// <param name="parts">The collection of parts.</param>
        /// <returns>The <see cref="RoutePatternPathSegment"/>.</returns>
        public static RoutePatternPathSegment Segment(IEnumerable<RoutePatternPart> parts)
        {
            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }

            return SegmentCore(parts);
        }

        /// <summary>
        /// Creates a <see cref="RoutePatternPathSegment"/> from the provided collection
        /// of parts.
        /// </summary>
        /// <param name="parts">The collection of parts.</param>
        /// <returns>The <see cref="RoutePatternPathSegment"/>.</returns>
        public static RoutePatternPathSegment Segment(params RoutePatternPart[] parts)
        {
            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }

            return SegmentCore(parts);
        }

        private static RoutePatternPathSegment SegmentCore(IEnumerable<RoutePatternPart> parts)
        {
            return new RoutePatternPathSegment(parts.ToArray());
        }

        /// <summary>
        /// Creates a <see cref="RoutePatternLiteralPart"/> from the provided text
        /// content.
        /// </summary>
        /// <param name="content">The text content.</param>
        /// <returns>The <see cref="RoutePatternLiteralPart"/>.</returns>
        public static RoutePatternLiteralPart LiteralPart(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(content));
            }

            if (content.IndexOf('?') >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidLiteral(content));
            }

            return LiteralPartCore(content);
        }

        private static RoutePatternLiteralPart LiteralPartCore(string content)
        {
            return new RoutePatternLiteralPart(content);
        }

        /// <summary>
        /// Creates a <see cref="RoutePatternSeparatorPart"/> from the provided text
        /// content.
        /// </summary>
        /// <param name="content">The text content.</param>
        /// <returns>The <see cref="RoutePatternSeparatorPart"/>.</returns>
        public static RoutePatternSeparatorPart SeparatorPart(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(content));
            }

            return SeparatorPartCore(content);
        }

        private static RoutePatternSeparatorPart SeparatorPartCore(string content)
        {
            return new RoutePatternSeparatorPart(content);
        }

        /// <summary>
        /// Creates a <see cref="RoutePatternParameterPart"/> from the provided parameter name.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The <see cref="RoutePatternParameterPart"/>.</returns>
        public static RoutePatternParameterPart ParameterPart(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(parameterName));
            }

            if (parameterName.IndexOfAny(RoutePatternParser.InvalidParameterNameChars) >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidParameterName(parameterName));
            }

            return ParameterPartCore(
                parameterName: parameterName,
                @default: null,
                parameterKind: RoutePatternParameterKind.Standard,
                constraints: Array.Empty<RoutePatternConstraintReference>());
        }

        /// <summary>
        /// Creates a <see cref="RoutePatternParameterPart"/> from the provided parameter name
        /// and default value.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="default">The parameter default value. May be <c>null</c>.</param>
        /// <returns>The <see cref="RoutePatternParameterPart"/>.</returns>
        public static RoutePatternParameterPart ParameterPart(string parameterName, object @default)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(parameterName));
            }

            if (parameterName.IndexOfAny(RoutePatternParser.InvalidParameterNameChars) >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidParameterName(parameterName));
            }

            return ParameterPartCore(
                parameterName: parameterName,
                @default: @default,
                parameterKind: RoutePatternParameterKind.Standard,
                constraints: Array.Empty<RoutePatternConstraintReference>());
        }

        /// <summary>
        /// Creates a <see cref="RoutePatternParameterPart"/> from the provided parameter name
        /// and default value, and parameter kind.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="default">The parameter default value. May be <c>null</c>.</param>
        /// <param name="parameterKind">The parameter kind.</param>
        /// <returns>The <see cref="RoutePatternParameterPart"/>.</returns>
        public static RoutePatternParameterPart ParameterPart(
            string parameterName,
            object @default,
            RoutePatternParameterKind parameterKind)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(parameterName));
            }

            if (parameterName.IndexOfAny(RoutePatternParser.InvalidParameterNameChars) >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidParameterName(parameterName));
            }

            if (@default != null && parameterKind == RoutePatternParameterKind.Optional)
            {
                throw new ArgumentNullException(Resources.TemplateRoute_OptionalCannotHaveDefaultValue, nameof(parameterKind));
            }

            return ParameterPartCore(
                parameterName: parameterName,
                @default: @default,
                parameterKind: parameterKind,
                constraints: Array.Empty<RoutePatternConstraintReference>());
        }

        /// <summary>
        /// Creates a <see cref="RoutePatternParameterPart"/> from the provided parameter name
        /// and default value, parameter kind, and constraints.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="default">The parameter default value. May be <c>null</c>.</param>
        /// <param name="parameterKind">The parameter kind.</param>
        /// <param name="constraints">The constraints to associated with the parameter.</param>
        /// <returns>The <see cref="RoutePatternParameterPart"/>.</returns>
        public static RoutePatternParameterPart ParameterPart(
            string parameterName,
            object @default,
            RoutePatternParameterKind parameterKind,
            IEnumerable<RoutePatternConstraintReference> constraints)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(parameterName));
            }

            if (parameterName.IndexOfAny(RoutePatternParser.InvalidParameterNameChars) >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidParameterName(parameterName));
            }

            if (@default != null && parameterKind == RoutePatternParameterKind.Optional)
            {
                throw new ArgumentNullException(Resources.TemplateRoute_OptionalCannotHaveDefaultValue, nameof(parameterKind));
            }

            if (constraints == null)
            {
                throw new ArgumentNullException(nameof(constraints));
            }

            return ParameterPartCore(
                parameterName: parameterName,
                @default: @default,
                parameterKind: parameterKind,
                constraints: constraints);
        }

        /// <summary>
        /// Creates a <see cref="RoutePatternParameterPart"/> from the provided parameter name
        /// and default value, parameter kind, and constraints.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="default">The parameter default value. May be <c>null</c>.</param>
        /// <param name="parameterKind">The parameter kind.</param>
        /// <param name="constraints">The constraints to associated with the parameter.</param>
        /// <returns>The <see cref="RoutePatternParameterPart"/>.</returns>
        public static RoutePatternParameterPart ParameterPart(
            string parameterName,
            object @default,
            RoutePatternParameterKind parameterKind,
            params RoutePatternConstraintReference[] constraints)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(parameterName));
            }

            if (parameterName.IndexOfAny(RoutePatternParser.InvalidParameterNameChars) >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidParameterName(parameterName));
            }

            if (@default != null && parameterKind == RoutePatternParameterKind.Optional)
            {
                throw new ArgumentNullException(Resources.TemplateRoute_OptionalCannotHaveDefaultValue, nameof(parameterKind));
            }

            if (constraints == null)
            {
                throw new ArgumentNullException(nameof(constraints));
            }

            return ParameterPartCore(
                parameterName: parameterName,
                @default: @default,
                parameterKind: parameterKind,
                constraints: constraints);
        }

        private static RoutePatternParameterPart ParameterPartCore(
            string parameterName,
            object @default,
            RoutePatternParameterKind parameterKind,
            IEnumerable<RoutePatternConstraintReference> constraints)
        {
            return new RoutePatternParameterPart(parameterName, @default, parameterKind, constraints.ToArray());
        }

        /// <summary>
        /// Creates a <see cref="RoutePatternConstraintReference"/> from the provided object.
        /// </summary>
        /// <param name="constraint">
        /// The constraint object, which must be of type <see cref="IRouteConstraint"/>,
        /// <see cref="MatcherPolicy"/>, or <see cref="String"/>. If the constraint object
        /// is a <see cref="String"/> it will be tranformed into an instance of 
        /// <see cref="RegexRouteConstraint"/>.
        /// </param>
        /// <returns>The <see cref="RoutePatternConstraintReference"/>.</returns>
        public static RoutePatternConstraintReference Constraint(object constraint)
        {
            // Similar to RouteConstraintBuilder
            if (constraint is IRouteConstraint routeConstraint)
            {
                return ConstraintCore(routeConstraint);
            }
            else if (constraint is MatchProcessor matchProcessor)
            {
                return ConstraintCore(matchProcessor);
            }
            else if (constraint is string content)
            {
                return ConstraintCore(new RegexRouteConstraint("^(" + content + ")$"));
            }
            else
            {
                throw new InvalidOperationException(Resources.FormatRoutePattern_InvalidConstraintReference(
                    constraint ?? "null",
                    typeof(IRouteConstraint),
                    typeof(MatchProcessor)));
            }
        }

        /// <summary>
        /// Creates a <see cref="RoutePatternConstraintReference"/> from the provided object.
        /// </summary>
        /// <param name="constraint">
        /// The constraint object.
        /// </param>
        /// <returns>The <see cref="RoutePatternConstraintReference"/>.</returns>
        public static RoutePatternConstraintReference Constraint(IRouteConstraint constraint)
        {
            if (constraint == null)
            {
                throw new ArgumentNullException(nameof(constraint));
            }

            return ConstraintCore(constraint);
        }

        /// <summary>
        /// Creates a <see cref="RoutePatternConstraintReference"/> from the provided object.
        /// </summary>
        /// <param name="matchProcessor">
        /// The match processor object.
        /// </param>
        /// <returns>The <see cref="RoutePatternConstraintReference"/>.</returns>
        public static RoutePatternConstraintReference Constraint(MatchProcessor matchProcessor)
        {
            if (matchProcessor == null)
            {
                throw new ArgumentNullException(nameof(matchProcessor));
            }

            return ConstraintCore(matchProcessor);
        }

        /// <summary>
        /// Creates a <see cref="RoutePatternConstraintReference"/> from the provided object.
        /// </summary>
        /// <param name="constraint">
        /// The constraint text, which will be tranformed into an instance of 
        /// <see cref="RegexRouteConstraint"/>.
        /// </param>
        /// <returns>The <see cref="RoutePatternConstraintReference"/>.</returns>
        public static RoutePatternConstraintReference Constraint(string constraint)
        {
            if (string.IsNullOrEmpty(constraint))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(constraint));
            }

            return ConstraintCore(constraint);
        }

        private static RoutePatternConstraintReference ConstraintCore(string constraint)
        {
            return new RoutePatternConstraintReference(constraint);
        }

        private static RoutePatternConstraintReference ConstraintCore(IRouteConstraint constraint)
        {
            return new RoutePatternConstraintReference(constraint);
        }

        private static RoutePatternConstraintReference ConstraintCore(MatchProcessor matchProcessor)
        {
            return new RoutePatternConstraintReference(matchProcessor);
        }
    }
}
