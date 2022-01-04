// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Static class that adds extension methods to <see cref="ElementReference"/>.
/// </summary>
public static class ElementReferenceExtensions
{
    /// <summary>
    /// Gives focus to an element given its <see cref="ElementReference"/>.
    /// </summary>
    /// <param name="elementReference">A reference to the element to focus.</param>
    /// <returns>The <see cref="ValueTask"/> representing the asynchronous focus operation.</returns>
    public static ValueTask FocusAsync(this ElementReference elementReference) => elementReference.FocusAsync(preventScroll: false);

    /// <summary>
    /// Gives focus to an element given its <see cref="ElementReference"/>.
    /// </summary>
    /// <param name="elementReference">A reference to the element to focus.</param>
    /// <param name="preventScroll">
    /// <para>
    ///     A <see cref="bool" /> value indicating whether or not the browser should scroll the document to bring the newly-focused element into view.
    ///     A value of false for preventScroll (the default) means that the browser will scroll the element into view after focusing it.
    ///     If preventScroll is set to true, no scrolling will occur.
    /// </para>
    /// </param>
    /// <returns>The <see cref="ValueTask"/> representing the asynchronous focus operation.</returns>
    public static ValueTask FocusAsync(this ElementReference elementReference, bool preventScroll)
    {
        var jsRuntime = elementReference.GetJSRuntime();

        if (jsRuntime == null)
        {
            throw new InvalidOperationException("No JavaScript runtime found.");
        }

        return jsRuntime.InvokeVoidAsync(DomWrapperInterop.Focus, elementReference, preventScroll);
    }

    internal static IJSRuntime GetJSRuntime(this ElementReference elementReference)
    {
        if (!(elementReference.Context is WebElementReferenceContext context))
        {
            throw new InvalidOperationException("ElementReference has not been configured correctly.");
        }

        return context.JSRuntime;
    }
}
