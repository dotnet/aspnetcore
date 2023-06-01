// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal interface IFormDataConverterFactory
{
    public bool CanConvert(Type type, FormDataMapperOptions options);

    public FormDataConverter CreateConverter(Type type, FormDataMapperOptions options);
}
