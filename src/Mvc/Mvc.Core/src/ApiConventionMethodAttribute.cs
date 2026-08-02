// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// API conventions to be applied to a controller action.
/// <para>
/// API conventions are used to influence the output of ApiExplorer.
/// <see cref="ApiConventionMethodAttribute"/> can be used to specify an exact convention method that applies
/// to an action. <see cref="ApiConventionTypeAttribute"/> for details about applying conventions at
/// the assembly or controller level.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ApiConventionMethodAttribute : Attribute
{
    /// <summary>
    /// Initializes an <see cref="ApiConventionMethodAttribute"/> instance using <paramref name="conventionType"/> and
    /// the specified <paramref name="methodName"/>.
    /// </summary>
    /// <param name="conventionType">
    /// The <see cref="Type"/> of the convention.
    /// <para>
    /// Conventions must be static types. Methods in a convention are
    /// matched to an action method using rules specified by <see cref="ApiConventionNameMatchAttribute" />
    /// that may be applied to a method name or its parameters and <see cref="ApiConventionTypeMatchAttribute"/>
    /// that are applied to parameters.
    /// </para>
    /// </param>
    /// <param name="methodName">The method name.</param>
    public ApiConventionMethodAttribute(Type conventionType, string methodName)
    {
        ConventionType = conventionType ?? throw new ArgumentNullException(nameof(conventionType));
        ApiConventionTypeAttribute.EnsureValid(conventionType);

        ArgumentException.ThrowIfNullOrEmpty(methodName);

        Method = GetConventionMethod(conventionType, methodName);
    }

    private static MethodInfo GetConventionMethod(Type conventionType, string methodName)
    {
        var methods = conventionType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == methodName)
            .ToArray();

        if (methods.Length == 0)
        {
            throw new ArgumentException(Resources.FormatApiConventionMethod_NoMethodFound(methodName, conventionType), nameof(methodName));
        }
        else if (methods.Length > 1)
        {
            throw new ArgumentException(Resources.FormatApiConventionMethod_AmbiguousMethodName(methodName, conventionType), nameof(methodName));
        }

        return methods[0];
    }

    /// <summary>
    /// Gets the convention type.
    /// </summary>
    public Type ConventionType { get; }

    internal MethodInfo Method { get; }
}
