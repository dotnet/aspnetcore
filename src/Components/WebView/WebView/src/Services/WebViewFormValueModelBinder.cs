// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms.ModelBinding;

namespace Microsoft.AspNetCore.Components.WebView.Services;

internal class WebViewFormValueModelBinder : IFormValueModelBinder
{
    public bool CanBind(Type valueType, string formName = null) => false;
    public void Bind(FormValueModelBindingContext context) { }
}
