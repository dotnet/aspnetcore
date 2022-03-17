// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Microsoft.AspNetCore.Cors;

/// <inheritdoc />
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class EnableCorsAttribute : Attribute, IEnableCorsAttribute
{
    /// <summary>
    /// Creates a new instance of the <see cref="EnableCorsAttribute"/> with the default policy
    /// name defined by <see cref="CorsOptions.DefaultPolicyName"/>.
    /// </summary>
    public EnableCorsAttribute()
        : this(policyName: null)
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="EnableCorsAttribute"/> with the supplied policy name.
    /// </summary>
    /// <param name="policyName">The name of the policy to be applied.</param>
    public EnableCorsAttribute(string? policyName)
    {
        PolicyName = policyName;
    }

    /// <inheritdoc />
    public string? PolicyName { get; set; }
}
