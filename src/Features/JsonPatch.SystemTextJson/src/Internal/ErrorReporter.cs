// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Exceptions;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Internal;

internal static class ErrorReporter
{
    public static readonly Action<JsonPatchError> Default = (error) =>
    {
        throw new JsonPatchException(error);
    };
}
