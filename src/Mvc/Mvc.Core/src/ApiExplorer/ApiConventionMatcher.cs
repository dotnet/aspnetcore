// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

internal static class ApiConventionMatcher
{
    internal static bool IsMatch(MethodInfo methodInfo, MethodInfo conventionMethod)
    {
        return MethodMatches() && ParametersMatch();

        bool MethodMatches()
        {
            var methodNameMatchBehavior = GetNameMatchBehavior(conventionMethod);
            return IsNameMatch(methodInfo.Name, conventionMethod.Name, methodNameMatchBehavior);
        }

        bool ParametersMatch()
        {
            var methodParameters = methodInfo.GetParameters();
            var conventionMethodParameters = conventionMethod.GetParameters();

            for (var i = 0; i < conventionMethodParameters.Length; i++)
            {
                var conventionParameter = conventionMethodParameters[i];
                if (conventionParameter.IsDefined(typeof(ParamArrayAttribute)))
                {
                    return true;
                }

                if (methodParameters.Length <= i)
                {
                    return false;
                }

                var nameMatchBehavior = GetNameMatchBehavior(conventionParameter);
                var typeMatchBehavior = GetTypeMatchBehavior(conventionParameter);

                if (!IsTypeMatch(methodParameters[i].ParameterType, conventionParameter.ParameterType, typeMatchBehavior) ||
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

    internal static ApiConventionNameMatchBehavior GetNameMatchBehavior(ICustomAttributeProvider attributeProvider)
    {
        var attribute = GetCustomAttribute<ApiConventionNameMatchAttribute>(attributeProvider);
        return attribute?.MatchBehavior ?? ApiConventionNameMatchBehavior.Exact;
    }

    internal static ApiConventionTypeMatchBehavior GetTypeMatchBehavior(ICustomAttributeProvider attributeProvider)
    {
        var attribute = GetCustomAttribute<ApiConventionTypeMatchAttribute>(attributeProvider);
        return attribute?.MatchBehavior ?? ApiConventionTypeMatchBehavior.AssignableFrom;
    }

    private static TAttribute? GetCustomAttribute<TAttribute>(ICustomAttributeProvider attributeProvider)
    {
        var attributes = attributeProvider.GetCustomAttributes(inherit: false);
        for (var i = 0; i < attributes.Length; i++)
        {
            if (attributes[i] is TAttribute attribute)
            {
                return attribute;
            }
        }

        return default;
    }

    internal static bool IsNameMatch(string? name, string? conventionName, ApiConventionNameMatchBehavior nameMatchBehavior)
    {
        switch (nameMatchBehavior)
        {
            case ApiConventionNameMatchBehavior.Any:
                return true;

            case ApiConventionNameMatchBehavior.Exact:
                return string.Equals(name, conventionName, StringComparison.Ordinal);

            case ApiConventionNameMatchBehavior.Prefix:
                return IsNameMatchPrefix();

            case ApiConventionNameMatchBehavior.Suffix:
                return IsNameMatchSuffix();

            default:
                return false;
        }

        bool IsNameMatchPrefix()
        {
            if (name is null || conventionName is null)
            {
                return false;
            }

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
            if (name is null || conventionName is null)
            {
                return false;
            }

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

    internal static bool IsTypeMatch(Type type, Type conventionType, ApiConventionTypeMatchBehavior typeMatchBehavior)
    {
        switch (typeMatchBehavior)
        {
            case ApiConventionTypeMatchBehavior.Any:
                return true;

            case ApiConventionTypeMatchBehavior.AssignableFrom:
                return conventionType.IsAssignableFrom(type);

            default:
                return false;
        }
    }
}
