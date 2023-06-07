// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal abstract class ComplexTypeExpressionConverterFactory
{
    internal abstract FormDataConverter CreateConverter(Type type, FormDataMapperOptions options);
}
