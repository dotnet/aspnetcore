// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Template
{
    [DebuggerDisplay("{DebuggerToString()}")]
    public class TemplatePart
    {
        public TemplatePart()
        {
        }

        public TemplatePart(RoutePatternPart other)
        {
            IsLiteral = other.IsLiteral || other.IsSeparator;
            IsParameter = other.IsParameter;

            if (other.IsLiteral && other is RoutePatternLiteralPart literal)
            {
                Text = literal.Content;
            }
            else if (other.IsParameter && other is RoutePatternParameterPart parameter)
            {
                // Text is unused by TemplatePart and assumed to be null when the part is a parameter.
                Name = parameter.Name;
                IsCatchAll = parameter.IsCatchAll;
                IsOptional = parameter.IsOptional;
                DefaultValue = parameter.Default;
                InlineConstraints = parameter.ParameterPolicies?.Select(p => new InlineConstraint(p));
            }
            else if (other.IsSeparator && other is RoutePatternSeparatorPart separator)
            {
                Text = separator.Content;
                IsOptionalSeperator = true;
            }
            else
            {
                // Unreachable
                throw new NotSupportedException();
            }
        }

        public static TemplatePart CreateLiteral(string text)
        {
            return new TemplatePart()
            {
                IsLiteral = true,
                Text = text,
            };
        }

        public static TemplatePart CreateParameter(
            string name,
            bool isCatchAll,
            bool isOptional,
            object defaultValue,
            IEnumerable<InlineConstraint> inlineConstraints)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return new TemplatePart()
            {
                IsParameter = true,
                Name = name,
                IsCatchAll = isCatchAll,
                IsOptional = isOptional,
                DefaultValue = defaultValue,
                InlineConstraints = inlineConstraints ?? Enumerable.Empty<InlineConstraint>(),
            };
        }

        public bool IsCatchAll { get; private set; }
        public bool IsLiteral { get; private set; }
        public bool IsParameter { get; private set; }
        public bool IsOptional { get; private set; }
        public bool IsOptionalSeperator { get; set; }
        public string Name { get; private set; }
        public string Text { get; private set; }
        public object DefaultValue { get; private set; }
        public IEnumerable<InlineConstraint> InlineConstraints { get; private set; }

        internal string DebuggerToString()
        {
            if (IsParameter)
            {
                return "{" + (IsCatchAll ? "*" : string.Empty) + Name + (IsOptional ? "?" : string.Empty) + "}";
            }
            else
            {
                return Text;
            }
        }

        public RoutePatternPart ToRoutePatternPart()
        {
            if (IsLiteral && IsOptionalSeperator)
            {
                return RoutePatternFactory.SeparatorPart(Text);
            }
            else if (IsLiteral)
            {
                return RoutePatternFactory.LiteralPart(Text);
            }
            else
            {
                var kind = IsCatchAll ?
                    RoutePatternParameterKind.CatchAll :
                    IsOptional ?
                        RoutePatternParameterKind.Optional :
                        RoutePatternParameterKind.Standard;

                var constraints = InlineConstraints.Select(c => new RoutePatternParameterPolicyReference(c.Constraint));
                return RoutePatternFactory.ParameterPart(Name, DefaultValue, kind, constraints);
            }
        }
    }
}
