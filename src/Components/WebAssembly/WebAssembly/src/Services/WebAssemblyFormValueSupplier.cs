// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Binding;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;
internal class WebAssemblyFormValueSupplier : IFormValueSupplier
{
    public bool CanBind(Type valueType, string? formName = null) => false;
    public void Bind(FormValueSupplierContext context) { }
}
