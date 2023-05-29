// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal abstract class FormDataConverter<T> : FormDataConverter
{
    internal abstract bool TryRead(ref FormDataReader context, Type type, FormDataMapperOptions options, out T? result, out bool found);
}
