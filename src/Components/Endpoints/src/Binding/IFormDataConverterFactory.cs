// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal interface IFormDataConverterFactory
{
    public static abstract bool CanConvert(Type type, FormDataMapperOptions options);

    public static abstract FormDataConverter CreateConverter(Type type, FormDataMapperOptions options);
}
