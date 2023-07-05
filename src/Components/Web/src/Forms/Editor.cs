// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// A component used for editing a value of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class Editor<T> : ComponentBase
{
    private HtmlFieldPrefix? _value;

    /// <summary>
    /// The value for the component.
    /// </summary>
    [Parameter] public T Value { get; set; } = default!;

    /// <summary>
    /// An expression that represents the value for the component.
    /// </summary>
    [Parameter] public Expression<Func<T>> ValueExpression { get; set; } = default!;

    /// <summary>
    /// A callback that gets invoked when the value changes.
    /// </summary>
    [Parameter] public EventCallback<T> ValueChanged { get; set; } = default!;

    [CascadingParameter] private HtmlFieldPrefix FieldPrefix { get; set; } = default!;

    /// <summary>
    /// Returns the name for the specified <paramref name="expression"/> in the current context.
    /// </summary>
    /// <param name="expression">The expression to use to compute the name.</param>
    /// <returns>The name for the specified <paramref name="expression"/> in the current context.</returns>
    /// <remarks>The provided <paramref name="expression"/> must be a member expression with <see cref="Editor{T}.Value"/> as it source.</remarks>
    protected string NameFor(LambdaExpression expression) => _value!.GetFieldName(expression);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (ValueExpression == null)
        {
            throw new InvalidOperationException($"{GetType()} requires a value for the 'ValueExpression' " +
                "parameter. Normally this is provided automatically when using 'bind-Value'.");
        }

        _value = FieldPrefix != null ? FieldPrefix.Combine(ValueExpression) : new HtmlFieldPrefix(ValueExpression);
    }

    private protected override void RenderCore(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<HtmlFieldPrefix>>(0);
        builder.AddAttribute(1, "Value", _value);
        builder.AddAttribute(2, "IsFixed", true);
        builder.AddAttribute(3, "ChildContent", (RenderFragment)BuildRenderTree);
        builder.CloseComponent();
    }
}
