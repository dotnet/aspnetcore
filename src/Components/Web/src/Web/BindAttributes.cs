// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Infrastructure for the discovery of <c>bind</c> attributes for markup elements.
    /// </summary>
    /// <remarks>
    /// To extend the set of <c>bind</c> attributes, define a public class named
    /// <c>BindAttributes</c> and annotate it with the appropriate attributes.
    /// </remarks>

    // Handles cases like <input @bind="..." /> - this is a fallback and will be ignored
    // when a specific type attribute is applied.
    [BindInputElement(null, null, "value", "onchange", isInvariantCulture: false, format: null)]

    // Handles cases like <input @bind-value="..." /> - this is a fallback and will be ignored
    // when a specific type attribute is applied.
    [BindInputElement(null, "value", "value", "onchange", isInvariantCulture: false, format: null)]

    [BindInputElement("checkbox", null, "checked", "onchange", isInvariantCulture: false, format: null)]
    [BindInputElement("text", null, "value", "onchange", isInvariantCulture: false, format: null)]

    // type="number" is invariant culture
    [BindInputElement("number", null, "value", "onchange", isInvariantCulture: true, format: null)]
    [BindInputElement("number", "value", "value", "onchange", isInvariantCulture: true, format: null)]

    // type="date" is invariant culture with a specific format
    [BindInputElement("date", null, "value", "onchange", isInvariantCulture: true, format: "yyyy-MM-dd")]
    [BindInputElement("date", "value", "value", "onchange", isInvariantCulture: true, format: "yyyy-MM-dd")]

    // type="datetime-local" is invariant culture with a specific format.
    // See https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings for details.
    [BindInputElement("datetime-local", null, "value", "onchange", isInvariantCulture: true, format: "yyyy-MM-ddTHH:mm:ss")]
    [BindInputElement("datetime-local", "value", "value", "onchange", isInvariantCulture: true, format: "yyyy-MM-ddTHH:mm:ss")]

    // type="month" is invariant culture with a specific format.
    // See https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings for details.
    [BindInputElement("month", null, "value", "onchange", isInvariantCulture: true, format: "yyyy-MM")]
    [BindInputElement("month", "value", "value", "onchange", isInvariantCulture: true, format: "yyyy-MM")]

    // type="time" is invariant culture with a specific format.
    [BindInputElement("time", null, "value", "onchange", isInvariantCulture: true, format: "HH:mm:ss")]
    [BindInputElement("time", "value", "value", "onchange", isInvariantCulture: true, format: "HH:mm:ss")]

    [BindElement("select", null, "value", "onchange")]
    [BindElement("textarea", null, "value", "onchange")]
    public static class BindAttributes
    {
    }
}
