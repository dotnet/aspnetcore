// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Infrastructure for the discovery of <c>bind</c> attributes for markup elements.
    /// </summary>
    /// <remarks>
    /// To extend the set of <c>bind</c> attributes, define a public class named 
    /// <c>BindAttributes</c> and annotate it with the appropriate attributes.
    /// </remarks>
    
    // Handles cases like <input bind="..." /> - this is a fallback and will be ignored
    // when a specific type attribute is applied.
    [BindInputElement(null, null, "value", "onchange")]

    [BindInputElement("checkbox", null, "checked", "onchange")]
    [BindInputElement("text", null, "value", "onchange")]

    [BindElement("select", null, "value", "onchange")]
    [BindElement("textarea", null, "value", "onchange")]
    public static class BindAttributes
    {
    }
}
