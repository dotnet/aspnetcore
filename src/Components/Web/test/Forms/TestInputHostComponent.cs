// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms;

internal class TestInputHostComponent<TValue, TComponent> : AutoRenderComponent where TComponent : InputBase<TValue>
{
    public Dictionary<string, object> AdditionalAttributes { get; set; }

    public EditContext EditContext { get; set; }

    public TValue Value { get; set; }

    public Action<TValue> ValueChanged { get; set; }

    public Expression<Func<TValue>> ValueExpression { get; set; }

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
            childBuilder.AddMultipleAttributes(3, AdditionalAttributes);
            childBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }
}
