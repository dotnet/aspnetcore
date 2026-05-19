// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A marker interface for filters which define a policy for maximum size for the request body.
/// </summary>
public interface IRequestSizePolicy : IFilterMetadata
{
}
