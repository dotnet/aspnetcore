// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Defines the binding context for data bound from external sources.
/// </summary>
public sealed class CascadingModelBinder : IComponent, IDisposable
{
    private RenderHandle _handle;
    private ModelBindingContext? _bindingContext;
    private bool _hasPendingQueuedRender;

    /// <summary>
    /// The binding context name.
    /// </summary>
    [Parameter] public string Name { get; set; } = "";

    /// <summary>
    /// If true, indicates that <see cref="ModelBindingContext.BindingContextId"/> will not change.
    /// This is a performance optimization that allows the framework to skip setting up
    /// change notifications. Set this flag only if you will not change
    /// <see cref="Name"/> of this context or its parents' context during the component's lifetime.
    /// </summary>
    [Parameter] public bool IsFixed { get; set; }

    /// <summary>
    /// Specifies the content to be rendered inside this <see cref="CascadingModelBinder"/>.
    /// </summary>
    [Parameter] public RenderFragment<ModelBindingContext> ChildContent { get; set; } = default!;

    [CascadingParameter] ModelBindingContext? ParentContext { get; set; }

    [Inject] private NavigationManager Navigation { get; set; } = null!;

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _handle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        if (_bindingContext == null)
        {
            // First render
            Navigation.LocationChanged += HandleLocationChanged;
        }

        parameters.SetParameterProperties(this);
        if (ParentContext != null && string.IsNullOrEmpty(Name))
        {
            throw new InvalidOperationException($"Nested binding contexts must define a Name. (Parent context) = '{ParentContext.Name}'.");
        }

        UpdateBindingInformation(Navigation.Uri);
        Render();

        return Task.CompletedTask;
    }

    private void Render()
    {
        if (_hasPendingQueuedRender)
        {
            return;
        }
        _hasPendingQueuedRender = true;
        _handle.Render(builder =>
        {
            _hasPendingQueuedRender = false;
            builder.OpenComponent<CascadingValue<ModelBindingContext>>(0);
            builder.AddComponentParameter(1, nameof(CascadingValue<ModelBindingContext>.IsFixed), IsFixed);
            builder.AddComponentParameter(2, nameof(CascadingValue<ModelBindingContext>.Value), _bindingContext);
            builder.AddComponentParameter(3, nameof(CascadingValue<ModelBindingContext>.ChildContent), ChildContent?.Invoke(_bindingContext!));
            builder.CloseComponent();
        });
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var url = e.Location;
        UpdateBindingInformation(url);
        Render();
    }

    private void UpdateBindingInformation(string url)
    {
        // BindingContextId: action parameter used to define the handler
        // Name: form name and context used to bind
        // Cases:
        // 1) No name ("")
        // Name = "";
        // BindingContextId = "";
        // <form name="" action="" />
        // 2) Name provided
        // Name = "my-handler";
        // BindingContextId = <<base-relative-uri>>((<<existing-query>>&)|?)handler=my-handler
        // <form name="my-handler" action="relative/path?existing=value&handler=my-handler
        // 3) Parent has a name "parent-name"
        // Name = "parent-name.my-handler";
        // BindingContextId = <<base-relative-uri>>((<<existing-query>>&)|?)handler=my-handler
        var name = string.IsNullOrEmpty(ParentContext?.Name) ? Name : $"{ParentContext.Name}.{Name}";
        var bindingId = string.IsNullOrEmpty(name) ? "" : GenerateBindingContextId(name);

        var bindingContext = _bindingContext != null &&
            string.Equals(_bindingContext.Name, Name, StringComparison.Ordinal) &&
            string.Equals(_bindingContext.BindingContextId, bindingId, StringComparison.Ordinal) ?
            _bindingContext : new ModelBindingContext(name, bindingId);

        // It doesn't matter that we don't check IsFixed, since the CascadingValue we are setting up will throw if the app changes.
        if (IsFixed && _bindingContext != null && _bindingContext != bindingContext)
        {
            // Throw an exception if either the Name or the BindingContextId changed. Once a CascadingModelBinder has been initialized
            // as fixed, it can't change it's name nor its BindingContextId. This can happen in several situations:
            // * Component ParentContext hierarchy changes.
            //   * Technically, the component won't be retained in this case and will be destroyed instead.
            // * A parent changes Name.
            throw new InvalidOperationException($"'{nameof(CascadingModelBinder)}' 'Name' can't change after initialized.");
        }

        _bindingContext = bindingContext;

        string GenerateBindingContextId(string name)
        {
            var bindingId = Navigation.ToBaseRelativePath(Navigation.GetUriWithQueryParameter("handler", name));
            var hashIndex = bindingId.IndexOf('#');
            return hashIndex == -1 ? bindingId : new string(bindingId.AsSpan(0, hashIndex));
        }
    }

    void IDisposable.Dispose()
    {
        Navigation.LocationChanged -= HandleLocationChanged;
    }
}
