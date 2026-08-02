// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Internal;

internal class ConversionResult
{
    public ConversionResult(bool canBeConverted, object convertedInstance)
    {
        CanBeConverted = canBeConverted;
        ConvertedInstance = convertedInstance;
    }

    public bool CanBeConverted { get; }
    public object ConvertedInstance { get; }
}
