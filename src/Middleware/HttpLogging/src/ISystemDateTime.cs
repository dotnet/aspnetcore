// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HttpLogging;

internal interface ISystemDateTime
{
    /// <summary> 
    /// Retrieves the date and time currently set for this machine.
    /// </summary> 
    DateTime Now { get; }
}
