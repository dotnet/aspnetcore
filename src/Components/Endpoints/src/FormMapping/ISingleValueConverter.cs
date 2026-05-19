// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal interface ISingleValueConverter<T>
{
    bool CanConvertSingleValue();

    bool TryConvertValue(ref FormDataReader reader, string value, out T result);
}
