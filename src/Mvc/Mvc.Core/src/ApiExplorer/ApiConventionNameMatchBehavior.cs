// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <summary>
/// The behavior for matching the name of a convention parameter or method.
/// </summary>
public enum ApiConventionNameMatchBehavior
{
    /// <summary>
    /// Matches any name. Use this if the parameter does not need to be matched.
    /// </summary>
    Any,

    /// <summary>
    /// The parameter or method name must exactly match the convention.
    /// </summary>
    Exact,

    /// <summary>
    /// The parameter or method name in the convention is a proper prefix.
    /// <para>
    /// Casing is used to delineate words in a given name. For instance, with this behavior
    /// the convention name "Get" will match "Get", "GetPerson" or "GetById", but not "getById", "Getaway".
    /// </para>
    /// </summary>
    Prefix,

    /// <summary>
    /// The parameter or method name in the convention is a proper suffix.
    /// <para>
    /// Casing is used to delineate words in a given name. For instance, with this behavior
    /// the convention name "id" will match "id", or "personId" but not "grid" or "personid".
    /// </para>
    /// </summary>
    Suffix,
}
