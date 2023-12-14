// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Specifies options for use when enabling JS component support.
/// This type is not normally used directly from application code. In most cases, applications should
/// call methods on the <see cref="IJSComponentConfiguration" /> on their application host builder.
/// </summary>
public sealed class JSComponentConfigurationStore
{
    // Everything's internal here, and can only be operated upon via the extension methods on
    // IJSComponentConfiguration. This is so that, in the future, we can add any additional
    // configuration APIs (as further extension methods) and/or storage (as internal members here)
    // without needing any changes on the downstream code that implements IJSComponentConfiguration,
    // and without exposing any of the configuration storage across layers.

    private readonly Dictionary<string, Type> _jsComponentTypesByIdentifier = new(StringComparer.Ordinal);
    internal Dictionary<string, JSComponentParameter[]> JSComponentParametersByIdentifier { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, List<string>> JSComponentIdentifiersByInitializer { get; } = new(StringComparer.Ordinal);

    internal void Add([DynamicallyAccessedMembers(LinkerFlags.Component)] Type componentType, string identifier)
    {
        var parameterTypes = JSComponentInterop.GetComponentParameters(componentType).ParameterInfoByName;
        var parameters = new JSComponentParameter[parameterTypes.Count];
        var index = 0;
        foreach (var (name, type) in parameterTypes)
        {
            parameters[index++] = new JSComponentParameter(name, type.Type);
        }

        _jsComponentTypesByIdentifier.Add(identifier, componentType);
        JSComponentParametersByIdentifier.Add(identifier, parameters);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "Types added to dictionary are always correctly annotated.")]
    internal bool TryGetComponentType(
        string identifier,
        [NotNullWhen(true)][DynamicallyAccessedMembers(LinkerFlags.Component)] out Type? componentType)
    {
        return _jsComponentTypesByIdentifier.TryGetValue(identifier, out componentType);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(JSComponentParameter))]
    [DynamicDependency(nameof(WebRenderer.WebRendererInteropMethods.AddRootComponent), typeof(WebRenderer.WebRendererInteropMethods))]
    [DynamicDependency(nameof(WebRenderer.WebRendererInteropMethods.SetRootComponentParameters), typeof(WebRenderer.WebRendererInteropMethods))]
    [DynamicDependency(nameof(WebRenderer.WebRendererInteropMethods.RemoveRootComponent), typeof(WebRenderer.WebRendererInteropMethods))]
    internal void Add([DynamicallyAccessedMembers(LinkerFlags.Component)] Type componentType, string identifier, string javaScriptInitializer)
    {
        Add(componentType, identifier);

        if (string.IsNullOrEmpty(javaScriptInitializer))
        {
            throw new ArgumentException($"'{nameof(javaScriptInitializer)}' cannot be null or empty.", nameof(javaScriptInitializer));
        }

        // Since it has a JS initializer, prepare the metadata we'll supply to JS code
        if (!JSComponentIdentifiersByInitializer.TryGetValue(javaScriptInitializer, out var identifiersForInitializer))
        {
            identifiersForInitializer = new();
            JSComponentIdentifiersByInitializer.Add(javaScriptInitializer, identifiersForInitializer);
        }

        identifiersForInitializer.Add(identifier);
    }

    internal readonly struct JSComponentParameter
    {
        public readonly string Name { get; }
        public readonly string Type { get; }

        public JSComponentParameter(string name, Type dotNetType)
        {
            Name = name;
            Type = GetJSType(dotNetType);
        }

        private static string GetJSType(Type dotNetType) => dotNetType switch
        {
            var x when x == typeof(string) => "string",
            var x when x == typeof(bool) => "boolean",
            var x when x == typeof(bool?) => "boolean?",
            var x when x == typeof(decimal) => "number",
            var x when x == typeof(decimal?) => "number?",
            var x when x == typeof(double) => "number",
            var x when x == typeof(double?) => "number?",
            var x when x == typeof(float) => "number",
            var x when x == typeof(float?) => "number?",
            var x when x == typeof(int) => "number",
            var x when x == typeof(int?) => "number?",
            var x when x == typeof(long) => "number",
            var x when x == typeof(long?) => "number?",
            var x when JSComponentInterop.IsEventCallbackType(x) => "eventcallback",
            _ => "object"
        };
    }
}
