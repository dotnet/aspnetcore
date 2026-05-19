// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

/// <summary>
/// An <see cref="ApplicationPartFactory"/> that produces no parts.
/// <para>
/// This factory may be used to to preempt Mvc's default part discovery allowing for custom configuration at a later stage.
/// </para>
/// </summary>
public class NullApplicationPartFactory : ApplicationPartFactory
{
    /// <inheritdoc />
    public override IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly)
    {
        return Enumerable.Empty<ApplicationPart>();
    }
}
