// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal class FormDataMappingError
{
    internal FormDataMappingError(string key, FormattableString message, string? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(message);

        Key = key;
        Message = message;
        Value = value;
    }

    public string Key { get; }

    public FormattableString Message { get; }

    public string? Value { get; }
}
