// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

internal static class RouteValueDictionaryTrimmerWarning
{
    public const string Warning = "This API may perform reflection on supplied parameters which may be trimmed if not referenced directly. " +
        "Consider using a different overload to avoid this issue.";
}
