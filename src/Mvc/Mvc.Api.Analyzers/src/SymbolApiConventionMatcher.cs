// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    internal static class SymbolApiConventionMatcher
    {
        internal static bool IsMatch(ApiControllerSymbolCache symbolCache, IMethodSymbol method, IMethodSymbol conventionMethod)
        {
            return MethodMatches() && ParametersMatch();

            bool MethodMatches()
            {
                var methodNameMatchBehavior = GetNameMatchBehavior(symbolCache, conventionMethod);
                if (!IsNameMatch(method.Name, conventionMethod.Name, methodNameMatchBehavior))
                {
                    return false;
                }

                return true;
            }

            bool ParametersMatch()
            {
                var methodParameters = method.Parameters;
                var conventionMethodParameters = conventionMethod.Parameters;

                for (var i = 0; i < conventionMethodParameters.Length; i++)
                {
                    var conventionParameter = conventionMethodParameters[i];
                    if (conventionParameter.IsParams)
                    {
                        return true;
                    }

                    if (methodParameters.Length <= i)
                    {
                        return false;
                    }

                    var nameMatchBehavior = GetNameMatchBehavior(symbolCache, conventionParameter);
                    var typeMatchBehavior = GetTypeMatchBehavior(symbolCache, conventionParameter);

                    if (!IsTypeMatch(methodParameters[i].Type, conventionParameter.Type, typeMatchBehavior) ||
                        !IsNameMatch(methodParameters[i].Name, conventionParameter.Name, nameMatchBehavior))
                    {
                        return false;
                    }
                }

                // Ensure convention has at least as many parameters as the method. params convention argument are handled
                // inside the for loop.
                return methodParameters.Length == conventionMethodParameters.Length;
            }
        }

        internal static SymbolApiConventionNameMatchBehavior GetNameMatchBehavior(ApiControllerSymbolCache symbolCache, ISymbol symbol)
        {
            var attribute = symbol.GetAttributes(symbolCache.ApiConventionNameMatchAttribute).FirstOrDefault();
            if (attribute == null ||
                attribute.ConstructorArguments.Length != 1 ||
                attribute.ConstructorArguments[0].Kind != TypedConstantKind.Enum)
            {
                return SymbolApiConventionNameMatchBehavior.Exact;
            }

            var argEnum = attribute.ConstructorArguments[0].Value;

            if (argEnum == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            var intValue = (int)argEnum;

            return (SymbolApiConventionNameMatchBehavior)intValue;
        }

        internal static SymbolApiConventionTypeMatchBehavior GetTypeMatchBehavior(ApiControllerSymbolCache symbolCache, ISymbol symbol)
        {
            var attribute = symbol.GetAttributes(symbolCache.ApiConventionTypeMatchAttribute).FirstOrDefault();
            if (attribute == null ||
                attribute.ConstructorArguments.Length != 1 ||
                attribute.ConstructorArguments[0].Kind != TypedConstantKind.Enum)
            {
                return SymbolApiConventionTypeMatchBehavior.AssignableFrom;
            }

            var argEnum = attribute.ConstructorArguments[0].Value;

            if (argEnum == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            var intValue = (int)argEnum;

            return (SymbolApiConventionTypeMatchBehavior)intValue;
        }

        internal static bool IsNameMatch(string name, string conventionName, SymbolApiConventionNameMatchBehavior nameMatchBehavior)
        {
            switch (nameMatchBehavior)
            {
                case SymbolApiConventionNameMatchBehavior.Any:
                    return true;

                case SymbolApiConventionNameMatchBehavior.Exact:
                    return string.Equals(name, conventionName, StringComparison.Ordinal);

                case SymbolApiConventionNameMatchBehavior.Prefix:
                    return IsNameMatchPrefix();

                case SymbolApiConventionNameMatchBehavior.Suffix:
                    return IsNameMatchSuffix();

                default:
                    return false;
            }

            bool IsNameMatchPrefix()
            {
                if (name.Length < conventionName.Length)
                {
                    return false;
                }

                if (name.Length == conventionName.Length)
                {
                    // name = "Post", conventionName = "Post"
                    return string.Equals(name, conventionName, StringComparison.Ordinal);
                }

                if (!name.StartsWith(conventionName, StringComparison.Ordinal))
                {
                    // name = "GetPerson", conventionName = "Post"
                    return false;
                }

                // Check for name = "PostPerson", conventionName = "Post"
                // Verify the first letter after the convention name is upper case. In this case 'P' from "Person"
                return char.IsUpper(name[conventionName.Length]);
            }

            bool IsNameMatchSuffix()
            {
                if (name.Length < conventionName.Length)
                {
                    // name = "person", conventionName = "personName"
                    return false;
                }

                if (name.Length == conventionName.Length)
                {
                    // name = id, conventionName = id
                    return string.Equals(name, conventionName, StringComparison.Ordinal);
                }

                // Check for name = personName, conventionName = name
                var index = name.Length - conventionName.Length - 1;
                if (!char.IsLower(name[index]))
                {
                    // Verify letter before "name" is lowercase. In this case the letter 'n' at the end of "person"
                    return false;
                }

                index++;
                if (name[index] != char.ToUpperInvariant(conventionName[0]))
                {
                    // Verify the first letter from convention is upper case. In this case 'n' from "name"
                    return false;
                }

                // Match the remaining letters with exact case. i.e. match "ame" from "personName", "name"
                index++;
                return string.Compare(name, index, conventionName, 1, conventionName.Length - 1, StringComparison.Ordinal) == 0;
            }
        }

        internal static bool IsTypeMatch(ITypeSymbol type, ITypeSymbol conventionType, SymbolApiConventionTypeMatchBehavior typeMatchBehavior)
        {
            switch (typeMatchBehavior)
            {
                case SymbolApiConventionTypeMatchBehavior.Any:
                    return true;

                case SymbolApiConventionTypeMatchBehavior.AssignableFrom:
                    return conventionType.IsAssignableFrom(type);

                default:
                    return false;
            }
        }

        internal enum SymbolApiConventionTypeMatchBehavior
        {
            Any,
            AssignableFrom
        }

        internal enum SymbolApiConventionNameMatchBehavior
        {
            Any,
            Exact,
            Prefix,
            Suffix,
        }
    }
}
