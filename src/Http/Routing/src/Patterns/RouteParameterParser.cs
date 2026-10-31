// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Patterns;

#if COMPONENTS
using Microsoft.AspNetCore.Components.Routing.Patterns;
#endif

internal static class RouteParameterParser
{
    // This code parses the inside of the route parameter
    //
    // Ex: {hello} - this method is responsible for parsing 'hello'
    // The factoring between this class and RoutePatternParser is due to legacy.
    public static RoutePatternParameterPart ParseRouteParameter(string parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        if (parameter.Length == 0)
        {
            return new RoutePatternParameterPart(string.Empty, null, RoutePatternParameterKind.Standard, Array.Empty<RoutePatternParameterPolicyReference>());
        }

        var startIndex = 0;
        var endIndex = parameter.Length - 1;
        var encodeSlashes = true;

        var parameterKind = RoutePatternParameterKind.Standard;

        if (parameter.StartsWith("**", StringComparison.Ordinal))
        {
            encodeSlashes = false;
            parameterKind = RoutePatternParameterKind.CatchAll;
            startIndex += 2;
        }
        else if (parameter[0] == '*')
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

        string? defaultValue = null;
        if (currentIndex <= endIndex &&
            parameter[currentIndex] == '=')
        {
            defaultValue = parameter.Substring(currentIndex + 1, endIndex - currentIndex);
        }

        return new RoutePatternParameterPart(
            parameterName,
            defaultValue,
            parameterKind,
            parseResults.ParameterPolicies,
            encodeSlashes);
    }

    private static ParameterPolicyParseResults ParseConstraints(
        string text,
        int currentIndex,
        int endIndex)
    {
#if !COMPONENTS
        var constraints = new ArrayBuilder<RoutePatternParameterPolicyReference>(0);
#else
        var constraints = new List<RoutePatternParameterPolicyReference>();
#endif
        var state = ParseState.Start;
        var startIndex = currentIndex;
        do
        {
            var currentChar = currentIndex > endIndex ? null : (char?)text[currentIndex];
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
                            var constraintText = text.Substring(startIndex, currentIndex - startIndex);
                            constraints.Add(RoutePatternFactory.ParameterPolicy(constraintText));
                            break;
                        case ')':
                            // Only consume a ')' token if
                            // (a) it is the last token
                            // (b) the next character is the start of the new constraint ':'
                            // (c) the next character is the start of the default value.

                            var nextChar = currentIndex + 1 > endIndex ? null : (char?)text[currentIndex + 1];
                            switch (nextChar)
                            {
                                case null:
                                    state = ParseState.End;
                                    constraintText = text.Substring(startIndex, currentIndex - startIndex + 1);
                                    constraints.Add(RoutePatternFactory.ParameterPolicy(constraintText));
                                    break;
                                case ':':
                                    state = ParseState.Start;
                                    constraintText = text.Substring(startIndex, currentIndex - startIndex + 1);
                                    constraints.Add(RoutePatternFactory.ParameterPolicy(constraintText));
                                    startIndex = currentIndex + 1;
                                    break;
                                case '=':
                                    state = ParseState.End;
                                    constraintText = text.Substring(startIndex, currentIndex - startIndex + 1);
                                    constraints.Add(RoutePatternFactory.ParameterPolicy(constraintText));
                                    break;
                            }
                            break;
                        case ':':
                        case '=':
                            // In the original implementation, the Regex would've backtracked if it encountered an
                            // unbalanced opening bracket followed by (not necessarily immediately) a delimiter.
                            // Simply verifying that the parentheses will eventually be closed should suffice to
                            // determine if the terminator needs to be consumed as part of the current constraint
                            // specification.
                            var indexOfClosingParantheses = text.IndexOf(')', currentIndex + 1);
                            if (indexOfClosingParantheses == -1)
                            {
                                constraintText = text.Substring(startIndex, currentIndex - startIndex);
                                constraints.Add(RoutePatternFactory.ParameterPolicy(constraintText));

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
                            var constraintText = text.Substring(startIndex, currentIndex - startIndex);
                            if (constraintText.Length > 0)
                            {
                                constraints.Add(RoutePatternFactory.ParameterPolicy(constraintText));
                            }
                            break;
                        case ':':
                            constraintText = text.Substring(startIndex, currentIndex - startIndex);
                            if (constraintText.Length > 0)
                            {
                                constraints.Add(RoutePatternFactory.ParameterPolicy(constraintText));
                            }
                            startIndex = currentIndex + 1;
                            break;
                        case '(':
                            state = ParseState.InsideParenthesis;
                            break;
                        case '=':
                            state = ParseState.End;
                            constraintText = text.Substring(startIndex, currentIndex - startIndex);
                            if (constraintText.Length > 0)
                            {
                                constraints.Add(RoutePatternFactory.ParameterPolicy(constraintText));
                            }
                            currentIndex--;
                            break;
                    }
                    break;
            }

            currentIndex++;

        } while (state != ParseState.End);

        return new ParameterPolicyParseResults(currentIndex, constraints.ToArray());
    }

    private enum ParseState
    {
        Start,
        ParsingName,
        InsideParenthesis,
        End
    }

    private readonly struct ParameterPolicyParseResults
    {
        public readonly int CurrentIndex;

        public readonly RoutePatternParameterPolicyReference[] ParameterPolicies;

        public ParameterPolicyParseResults(int currentIndex, RoutePatternParameterPolicyReference[] parameterPolicies)
        {
            CurrentIndex = currentIndex;
            ParameterPolicies = parameterPolicies;
        }
    }
}
