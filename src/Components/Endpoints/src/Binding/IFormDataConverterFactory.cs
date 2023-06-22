// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal interface IFormDataConverterFactory
{
    [RequiresDynamicCode(FormBindingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormBindingHelpers.RequiresUnreferencedCodeMessage)]
    public bool CanConvert(Type type, FormDataMapperOptions options);

    [RequiresDynamicCode(FormBindingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormBindingHelpers.RequiresUnreferencedCodeMessage)]
    public FormDataConverter CreateConverter(Type type, FormDataMapperOptions options);
}
