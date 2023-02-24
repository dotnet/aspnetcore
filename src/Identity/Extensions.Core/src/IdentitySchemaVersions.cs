// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Contains various identity version constants.
/// </summary>
public static class IdentitySchemaVersions
{
    /// <summary>
    /// Represents the default version of the identity schema 
    /// </summary>
    public static readonly Version Default = new Version(0, 0);

    /// <summary>
    /// Represents the initial 1.0 version of the identity schema 
    /// </summary>
    public static readonly Version Version1 = new Version(1, 0);

    /// <summary>
    /// Represents the 2.0 version of the identity schema
    /// </summary>
    public static readonly Version Version2 = new Version(2, 0);

}
