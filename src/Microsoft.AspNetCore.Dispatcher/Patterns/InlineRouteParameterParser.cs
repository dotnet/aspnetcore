// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Dispatcher.Patterns
{
    public static class InlineRouteParameterParser
    {
        public static RoutePatternParameter ParseRouteParameter(string text, string parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            if (parameter.Length == 0)
            {
                return new RoutePatternParameter(null, string.Empty, null, RoutePatternParameterKind.Standard, Array.Empty<ConstraintReference>());
            }

            var startIndex = 0;
            var endIndex = parameter.Length - 1;

            var parameterKind = RoutePatternParameterKind.Standard;
            if (parameter[0] == '*')
            {
                parameterKind = RoutePatternParameterKind.CatchAll;
                startIndex++;
            }

            if (parameter[endIndex] == '?')
            {
                parameterKind = RoutePatternParameterKind.Optional;
                endIndex--;
            }

            var currentIndex = startIndex;

            // Parse parameter name
            var parameterName = string.Empty;

            while (currentIndex <= endIndex)
            {
                var currentChar = parameter[currentIndex];

                if ((currentChar == ':' || currentChar == '=') && startIndex != currentIndex)
                {
                    // Parameter names are allowed to start with delimiters used to denote constraints or default values.
                    // i.e. "=foo" or ":bar" would be treated as parameter names rather than default value or constraint
                    // specifications.
                    parameterName = parameter.Substring(startIndex, currentIndex - startIndex);

                    // Roll the index back and move to the constraint parsing stage.
                    currentIndex--;
                    break;
                }
                else if (currentIndex == endIndex)
                {
                    parameterName = parameter.Substring(startIndex, currentIndex - startIndex + 1);
                }

                currentIndex++;
            }

            var parseResults = ParseConstraints(parameter, currentIndex, endIndex);
            currentIndex = parseResults.CurrentIndex;

            string defaultValue = null;
            if (currentIndex <= endIndex &&
                parameter[currentIndex] == '=')
            {
                defaultValue = parameter.Substring(currentIndex + 1, endIndex - currentIndex);
            }

            return new RoutePatternParameter(text, parameterName, defaultValue, parameterKind, parseResults.Constraints.ToArray());
        }

        private static ConstraintParseResults ParseConstraints(
            string parameter,
            int currentIndex,
            int endIndex)
        {
            var constraints = new List<ConstraintReference>();
            var state = ParseState.Start;
            var startIndex = currentIndex;
            do
            {
                var currentChar = currentIndex > endIndex ? null : (char?)parameter[currentIndex];
                switch (state)
                {
                    case ParseState.Start:
                        switch (currentChar)
                        {
                            case null:
                                state = ParseState.End;
                                break;
                            case ':':
                                state = ParseState.ParsingName;
                                startIndex = currentIndex + 1;
                                break;
                            case '(':
                                state = ParseState.InsideParenthesis;
                                break;
                            case '=':
                                state = ParseState.End;
                                currentIndex--;
                                break;
                        }
                        break;
                    case ParseState.InsideParenthesis:
                        switch (currentChar)
                        {
                            case null:
                                state = ParseState.End;
                                var constraintText = parameter.Substring(startIndex, currentIndex - startIndex);
                                constraints.Add(ConstraintReference.CreateFromText(constraintText, constraintText));
                                break;
                            case ')':
                                // Only consume a ')' token if
                                // (a) it is the last token
                                // (b) the next character is the start of the new constraint ':'
                                // (c) the next character is the start of the default value.

                                var nextChar = currentIndex + 1 > endIndex ? null : (char?)parameter[currentIndex + 1];
                                switch (nextChar)
                                {
                                    case null:
                                        state = ParseState.End;
                                        constraintText = parameter.Substring(startIndex, currentIndex - startIndex + 1);
                                        constraints.Add(ConstraintReference.CreateFromText(constraintText, constraintText));
                                        break;
                                    case ':':
                                        state = ParseState.Start;
                                        constraintText = parameter.Substring(startIndex, currentIndex - startIndex + 1);
                                        constraints.Add(ConstraintReference.CreateFromText(constraintText, constraintText));
                                        startIndex = currentIndex + 1;
                                        break;
                                    case '=':
                                        state = ParseState.End;
                                        constraintText = parameter.Substring(startIndex, currentIndex - startIndex + 1);
                                        constraints.Add(ConstraintReference.CreateFromText(constraintText, constraintText));
                                        break;
                                }
                                break;
                            case ':':
                            case '=':
                                // In the original implementation, the Regex would've backtracked if it encountered an
                                // unbalanced opening bracket followed by (not necessarily immediatiely) a delimiter.
                                // Simply verifying that the parantheses will eventually be closed should suffice to
                                // determine if the terminator needs to be consumed as part of the current constraint
                                // specification.
                                var indexOfClosingParantheses = parameter.IndexOf(')', currentIndex + 1);
                                if (indexOfClosingParantheses == -1)
                                {
                                    constraintText = parameter.Substring(startIndex, currentIndex - startIndex);
                                    constraints.Add(ConstraintReference.CreateFromText(constraintText, constraintText));

                                    if (currentChar == ':')
                                    {
                                        state = ParseState.ParsingName;
                                        startIndex = currentIndex + 1;
                                    }
                                    else
                                    {
                                        state = ParseState.End;
                                        currentIndex--;
                                    }
                                }
                                else
                                {
                                    currentIndex = indexOfClosingParantheses;
                                }

                                break;
                        }
                        break;
                    case ParseState.ParsingName:
                        switch (currentChar)
                        {
                            case null:
                                state = ParseState.End;
                                var constraintText = parameter.Substring(startIndex, currentIndex - startIndex);
                                constraints.Add(ConstraintReference.CreateFromText(constraintText, constraintText));
                                break;
                            case ':':
                                constraintText = parameter.Substring(startIndex, currentIndex - startIndex);
                                constraints.Add(ConstraintReference.CreateFromText(constraintText, constraintText));
                                startIndex = currentIndex + 1;
                                break;
                            case '(':
                                state = ParseState.InsideParenthesis;
                                break;
                            case '=':
                                state = ParseState.End;
                                constraintText = parameter.Substring(startIndex, currentIndex - startIndex);
                                constraints.Add(ConstraintReference.CreateFromText(constraintText, constraintText));
                                currentIndex--;
                                break;
                        }
                        break;
                }

                currentIndex++;

            } while (state != ParseState.End);

            return new ConstraintParseResults
            {
                CurrentIndex = currentIndex,
                Constraints = constraints
            };
        }

        private enum ParseState
        {
            Start,
            ParsingName,
            InsideParenthesis,
            End
        }

        private struct ConstraintParseResults
        {
            public int CurrentIndex;

            public IEnumerable<ConstraintReference> Constraints;
        }
    }
}