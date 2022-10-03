// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// 
/// </summary>
public interface IRequirementData
{
    /// <summary>
    /// </summary>
    /// <returns></returns>
    IEnumerable<IAuthorizationRequirement> GetRequirements();
}

/// <summary>
/// Specifies that the class or method that this attribute is applied to requires the specified authorization.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public abstract class RequirementAttribute : Attribute, IRequirementData
{
    /// <inheritdoc/>
    public abstract IEnumerable<IAuthorizationRequirement> GetRequirements();
}
