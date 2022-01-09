// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.FunctionalTests.Helpers;

public class StringsHelpers
{
    public static string GetResourceString(string stringName, params object[] parameters)
    {
        var strings = typeof(DatabaseErrorPageMiddleware).Assembly.GetType("Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Strings").GetTypeInfo();

        if (parameters.Length > 0)
        {
            var method = strings.GetDeclaredMethods(stringName).Single();
            return (string)method.Invoke(null, parameters);
        }

        return (string)strings.GetDeclaredProperty(stringName).GetValue(null);
    }
}
