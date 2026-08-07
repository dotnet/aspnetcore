// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// A host component used to render an <see cref="InputBase{TValue}"/> descendant
/// (e.g. <see cref="InputText"/>) inside an <see cref="EditContext"/>, where the set of
/// "extra" HTML attributes supplied is driven by the value of <see cref="IncludeAttribute"/>.
/// This mirrors the user code pattern from issue #56463 where conditional Razor
/// (<c>builder.AddAttribute(10, "class", value)</c>) calls may be omitted on a
/// subsequent render and the omitted attribute must be removed from the DOM.
/// </summary>
internal class TestInputConditionalAttributeHostComponent<TValue, TComponent> : AutoRenderComponent where TComponent : InputBase<TValue>
{
    public EditContext EditContext { get; set; }

    public TValue Value { get; set; }

    public Action<TValue> ValueChanged { get; set; }

    public Expression<Func<TValue>> ValueExpression { get; set; }

    /// <summary>
    /// When <see langword="true"/> (the default), the named attribute is added to the
    /// inner component's <c>AdditionalAttributes</c> dictionary. When <see langword="false"/>
    /// the attribute is omitted, allowing tests to verify that the omitted attribute is
    /// actually removed from the rendered DOM on the subsequent render.
    /// </summary>
    public bool IncludeAttribute { get; set; } = true;

    /// <summary>
    /// The attribute name (and value) supplied to the inner component when
    /// <see cref="IncludeAttribute"/> is <see langword="true"/>. Defaults to
    /// <c>"class"</c> to mirror the bug report in #56463.
    /// </summary>
    public string AttributeName { get; set; } = "class";

    public object AttributeValue { get; set; } = "example-class";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<EditContext>>(0);
        builder.AddComponentParameter(1, "Value", EditContext);
        builder.AddComponentParameter(2, "ChildContent", new RenderFragment(childBuilder =>
        {
            childBuilder.OpenComponent<TComponent>(0);
            childBuilder.AddComponentParameter(0, "Value", Value);
            childBuilder.AddComponentParameter(1, "ValueChanged",
                EventCallback.Factory.Create(this, ValueChanged));
            childBuilder.AddComponentParameter(2, "ValueExpression", ValueExpression);
            if (IncludeAttribute)
            {
                childBuilder.AddAttribute(3, AttributeName, AttributeValue);
            }
            childBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
