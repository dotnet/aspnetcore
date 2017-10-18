// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Dispatcher.Patterns
{
    public abstract class RoutePatternPart
    {
        // This class is not an extensibility point. It is abstract so we can extend it
        // or add semantics later inside the library.
        internal RoutePatternPart()
        {
        }

        public static RoutePatternLiteral CreateLiteral(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(content));
            }

            if (content.IndexOf('?') >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidLiteral(content));
            }

            return new RoutePatternLiteral(null, content);
        }

        public static RoutePatternLiteral CreateLiteralFromText(string rawText, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(content));
            }

            if (content.IndexOf('?') >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidLiteral(content));
            }

            return new RoutePatternLiteral(rawText, content);
        }

        public static RoutePatternSeparator CreateSeparator(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(content));
            }

            return new RoutePatternSeparator(null, content);
        }

        public static RoutePatternSeparator CreateSeparatorFromText(string rawText, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(content));
            }

            return new RoutePatternSeparator(rawText, content);
        }

        public static RoutePatternParameter CreateParameter(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(name));
            }

            if (name.IndexOfAny(RoutePatternParser.InvalidParameterNameChars) >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidParameterName(name));
            }

            return CreateParameterFromText(null, name, null, RoutePatternParameterKind.Standard, Array.Empty<ConstraintReference>());
        }

        public static RoutePatternParameter CreateParameterFromText(string rawText, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(name));
            }

            if (name.IndexOfAny(RoutePatternParser.InvalidParameterNameChars) >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidParameterName(name));
            }

            return CreateParameterFromText(rawText, name, null, RoutePatternParameterKind.Standard, Array.Empty<ConstraintReference>());
        }

        public static RoutePatternParameter CreateParameter(string name, object defaultValue)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(name));
            }

            if (name.IndexOfAny(RoutePatternParser.InvalidParameterNameChars) >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidParameterName(name));
            }

            return CreateParameterFromText(null, name, defaultValue, RoutePatternParameterKind.Standard, Array.Empty<ConstraintReference>());
        }

        public static RoutePatternParameter CreateParameterFromText(string rawText, string name, object defaultValue)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(name));
            }

            if (name.IndexOfAny(RoutePatternParser.InvalidParameterNameChars) >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidParameterName(name));
            }

            return CreateParameterFromText(rawText, name, defaultValue, RoutePatternParameterKind.Standard, Array.Empty<ConstraintReference>());
        }

        public static RoutePatternParameter CreateParameter(
            string name,
            object defaultValue,
            RoutePatternParameterKind parameterKind)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(name));
            }

            if (name.IndexOfAny(RoutePatternParser.InvalidParameterNameChars) >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidParameterName(name));
            }

            if (defaultValue != null && parameterKind == RoutePatternParameterKind.Optional)
            {
                throw new ArgumentNullException(Resources.TemplateRoute_OptionalCannotHaveDefaultValue, nameof(parameterKind));
            }

            return CreateParameterFromText(null, name, defaultValue, parameterKind, Array.Empty<ConstraintReference>());
        }

        public static RoutePatternParameter CreateParameterFromText(
            string rawText,
            string name, 
            object defaultValue, 
            RoutePatternParameterKind parameterKind)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(name));
            }

            if (name.IndexOfAny(RoutePatternParser.InvalidParameterNameChars) >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidParameterName(name));
            }

            if (defaultValue != null && parameterKind == RoutePatternParameterKind.Optional)
            {
                throw new ArgumentNullException(Resources.TemplateRoute_OptionalCannotHaveDefaultValue, nameof(parameterKind));
            }

            return CreateParameterFromText(rawText, name, defaultValue, parameterKind, Array.Empty<ConstraintReference>());
        }

        public static RoutePatternParameter CreateParameter(
            string name,
            object defaultValue,
            RoutePatternParameterKind parameterKind,
            params ConstraintReference[] constraints)
        {

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(name));
            }

            if (name.IndexOfAny(RoutePatternParser.InvalidParameterNameChars) >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidParameterName(name));
            }

            if (defaultValue != null && parameterKind == RoutePatternParameterKind.Optional)
            {
                throw new ArgumentNullException(Resources.TemplateRoute_OptionalCannotHaveDefaultValue, nameof(parameterKind));
            }

            return new RoutePatternParameter(null, name, defaultValue, parameterKind, constraints);
        }

        public static RoutePatternParameter CreateParameterFromText(
            string rawText,
            string name,
            object defaultValue,
            RoutePatternParameterKind parameterKind,
            params ConstraintReference[] constraints)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(Resources.Argument_NullOrEmpty, nameof(name));
            }

            if (name.IndexOfAny(RoutePatternParser.InvalidParameterNameChars) >= 0)
            {
                throw new ArgumentException(Resources.FormatTemplateRoute_InvalidParameterName(name));
            }

            if (defaultValue != null && parameterKind == RoutePatternParameterKind.Optional)
            {
                throw new ArgumentNullException(Resources.TemplateRoute_OptionalCannotHaveDefaultValue, nameof(parameterKind));
            }

            return new RoutePatternParameter(rawText, name, defaultValue, parameterKind, constraints);
        }

        public abstract RoutePatternPartKind PartKind { get; }

        public abstract string RawText { get; }

        public bool IsLiteral => PartKind == RoutePatternPartKind.Literal;

        public bool IsParameter => PartKind == RoutePatternPartKind.Parameter;

        public bool IsSeparator => PartKind == RoutePatternPartKind.Separator;

        internal abstract string DebuggerToString();
    }
}
