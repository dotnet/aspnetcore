// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// API conventions to be applied to an assembly containing MVC controllers or a single controller.
/// <para>
/// API conventions are used to influence the output of ApiExplorer.
/// Conventions must be static types. Methods in a convention are
/// matched to an action method using rules specified by <see cref="ApiConventionNameMatchAttribute" />
/// that may be applied to a method name or its parameters and <see cref="ApiConventionTypeMatchAttribute"/>
/// that are applied to parameters.
/// </para>
/// <para>
/// When no attributes are found specifying the behavior, MVC matches method names and parameter names are matched
/// using <see cref="ApiConventionNameMatchBehavior.Exact"/> and parameter types are matched
/// using <see cref="ApiConventionTypeMatchBehavior.AssignableFrom"/>.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class ApiConventionTypeAttribute : Attribute
{
    /// <summary>
    /// Initializes an <see cref="ApiConventionTypeAttribute"/> instance using <paramref name="conventionType"/>.
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
    public ApiConventionTypeAttribute(Type conventionType)
    {
        ConventionType = conventionType ?? throw new ArgumentNullException(nameof(conventionType));
        EnsureValid(conventionType);
    }

    /// <summary>
    /// Gets the convention type.
    /// </summary>
    public Type ConventionType { get; }

    internal static void EnsureValid(Type conventionType)
    {
        if (!conventionType.IsSealed || !conventionType.IsAbstract)
        {
            // Conventions must be static viz abstract + sealed.
            throw new ArgumentException(Resources.FormatApiConventionMustBeStatic(conventionType), nameof(conventionType));
        }

        foreach (var method in conventionType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            var unsupportedAttributes = method.GetCustomAttributes(inherit: true)
                .Where(attribute => !IsAllowedAttribute(attribute))
                .ToArray();

            if (unsupportedAttributes.Length == 0)
            {
                continue;
            }

            var methodDisplayName = TypeNameHelper.GetTypeDisplayName(method.DeclaringType!) + "." + method.Name;
            var errorMessage = Resources.FormatApiConvention_UnsupportedAttributesOnConvention(
                methodDisplayName,
                Environment.NewLine + string.Join(Environment.NewLine, unsupportedAttributes) + Environment.NewLine,
                $"{nameof(ProducesResponseTypeAttribute)}, {nameof(ProducesDefaultResponseTypeAttribute)}, {nameof(ApiConventionNameMatchAttribute)}");

            throw new ArgumentException(errorMessage, nameof(conventionType));
        }
    }

    private static bool IsAllowedAttribute(object attribute)
    {
        return attribute is ProducesResponseTypeAttribute ||
            attribute is ProducesDefaultResponseTypeAttribute ||
            attribute is ApiConventionNameMatchAttribute ||
            attribute.GetType().FullName == "System.Runtime.CompilerServices.NullableContextAttribute";
    }
}
