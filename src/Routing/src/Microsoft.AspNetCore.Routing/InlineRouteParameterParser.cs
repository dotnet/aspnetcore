// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing
{
    public static class InlineRouteParameterParser
    {
        public static TemplatePart ParseRouteParameter(string routeParameter)
        {
            if (routeParameter == null)
            {
                throw new ArgumentNullException(nameof(routeParameter));
            }

            if (routeParameter.Length == 0)
            {
                return TemplatePart.CreateParameter(
                    name: string.Empty,
                    isCatchAll: false,
                    isOptional: false,
                    defaultValue: null,
                    inlineConstraints: null);
            }

            var startIndex = 0;
            var endIndex = routeParameter.Length - 1;

            var isCatchAll = false;
            var isOptional = false;

            if (routeParameter[0] == '*')
            {
                isCatchAll = true;
                startIndex++;
            }

            if (routeParameter[endIndex] == '?')
            {
                isOptional = true;
                endIndex--;
            }

            var currentIndex = startIndex;

            // Parse parameter name
            var parameterName = string.Empty;

            while (currentIndex <= endIndex)
            {
                var currentChar = routeParameter[currentIndex];

                if ((currentChar == ':' || currentChar == '=') && startIndex != currentIndex)
                {
                    // Parameter names are allowed to start with delimiters used to denote constraints or default values.
                    // i.e. "=foo" or ":bar" would be treated as parameter names rather than default value or constraint
                    // specifications.
                    parameterName = routeParameter.Substring(startIndex, currentIndex - startIndex);

                    // Roll the index back and move to the constraint parsing stage.
                    currentIndex--;
                    break;
                }
                else if (currentIndex == endIndex)
                {
                    parameterName = routeParameter.Substring(startIndex, currentIndex - startIndex + 1);
                }

                currentIndex++;
            }

            var parseResults = ParseConstraints(routeParameter, currentIndex, endIndex);
            currentIndex = parseResults.CurrentIndex;

            string defaultValue = null;
            if (currentIndex <= endIndex &&
                routeParameter[currentIndex] == '=')
            {
                defaultValue = routeParameter.Substring(currentIndex + 1, endIndex - currentIndex);
            }

            return TemplatePart.CreateParameter(parameterName,
                                                isCatchAll,
                                                isOptional,
                                                defaultValue,
                                                parseResults.Constraints);
        }

        private static ConstraintParseResults ParseConstraints(
            string routeParameter,
            int currentIndex,
            int endIndex)
        {
            var inlineConstraints = new List<InlineConstraint>();
            var state = ParseState.Start;
            var startIndex = currentIndex;
            do
            {
                var currentChar = currentIndex > endIndex ? null : (char?)routeParameter[currentIndex];
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
                                var constraintText = routeParameter.Substring(startIndex, currentIndex - startIndex);
                                inlineConstraints.Add(new InlineConstraint(constraintText));
                                break;
                            case ')':
                                // Only consume a ')' token if
                                // (a) it is the last token
                                // (b) the next character is the start of the new constraint ':'
                                // (c) the next character is the start of the default value.

                                var nextChar = currentIndex + 1 > endIndex ? null : (char?)routeParameter[currentIndex + 1];
                                switch (nextChar)
                                {
                                    case null:
                                        state = ParseState.End;
                                        constraintText = routeParameter.Substring(startIndex, currentIndex - startIndex + 1);
                                        inlineConstraints.Add(new InlineConstraint(constraintText));
                                        break;
                                    case ':':
                                        state = ParseState.Start;
                                        constraintText = routeParameter.Substring(startIndex, currentIndex - startIndex + 1);
                                        inlineConstraints.Add(new InlineConstraint(constraintText));
                                        startIndex = currentIndex + 1;
                                        break;
                                    case '=':
                                        state = ParseState.End;
                                        constraintText = routeParameter.Substring(startIndex, currentIndex - startIndex + 1);
                                        inlineConstraints.Add(new InlineConstraint(constraintText));
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
                                var indexOfClosingParantheses = routeParameter.IndexOf(')', currentIndex + 1);
                                if (indexOfClosingParantheses == -1)
                                {
                                    constraintText = routeParameter.Substring(startIndex, currentIndex - startIndex);
                                    inlineConstraints.Add(new InlineConstraint(constraintText));

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
                                var constraintText = routeParameter.Substring(startIndex, currentIndex - startIndex);
                                inlineConstraints.Add(new InlineConstraint(constraintText));
                                break;
                            case ':':
                                constraintText = routeParameter.Substring(startIndex, currentIndex - startIndex);
                                inlineConstraints.Add(new InlineConstraint(constraintText));
                                startIndex = currentIndex + 1;
                                break;
                            case '(':
                                state = ParseState.InsideParenthesis;
                                break;
                            case '=':
                                state = ParseState.End;
                                constraintText = routeParameter.Substring(startIndex, currentIndex - startIndex);
                                inlineConstraints.Add(new InlineConstraint(constraintText));
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
                Constraints = inlineConstraints
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

            public IEnumerable<InlineConstraint> Constraints;
        }
    }
}