// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Interface that can produce authorization requirements.
/// </summary>
public interface IAuthorizationRequirementData
{
    /// <summary>
    /// Returns <see cref="IAuthorizationRequirement"/> that should be satisfied for authorization.
    /// </summary>
    /// <returns><see cref="IAuthorizationRequirement"/> used for authorization.</returns>
    IEnumerable<IAuthorizationRequirement> GetRequirements();
}
