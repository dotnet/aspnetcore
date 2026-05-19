// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HttpLogging;

internal sealed class SystemDateTime : ISystemDateTime
{
    public DateTime Now => DateTime.Now;
}
