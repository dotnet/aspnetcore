// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web.Infrastructure;

namespace Microsoft.AspNetCore.Components.Web
{
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

        internal Dictionary<string, Type> JsComponentTypesByIdentifier { get; } = new (StringComparer.Ordinal);
        internal Dictionary<string, List<JSComponentInfo>> JSComponentInfoByInitializer { get; } = new(StringComparer.Ordinal);

        internal void Add(Type componentType, string identifier)
        {
            JsComponentTypesByIdentifier.Add(identifier, componentType);
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(JSComponentInfo))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(JSComponentParameter))]
        [DynamicDependency(nameof(WebRenderer.WebRendererInteropMethods.AddRootComponent), typeof(WebRenderer.WebRendererInteropMethods))]
        [DynamicDependency(nameof(WebRenderer.WebRendererInteropMethods.SetRootComponentParameters), typeof(WebRenderer.WebRendererInteropMethods))]
        [DynamicDependency(nameof(WebRenderer.WebRendererInteropMethods.RemoveRootComponent), typeof(WebRenderer.WebRendererInteropMethods))]
        internal void Add(Type componentType, string identifier, string javaScriptInitializer)
        {
            Add(componentType, identifier);

            if (string.IsNullOrEmpty(javaScriptInitializer))
            {
                throw new ArgumentException($"'{nameof(javaScriptInitializer)}' cannot be null or empty.", nameof(javaScriptInitializer));
            }

            // Since it has a JS initializer, prepare the metadata we'll supply to JS code
            if (!JSComponentInfoByInitializer.TryGetValue(javaScriptInitializer, out var entriesForInitializer))
            {
                entriesForInitializer = new();
                JSComponentInfoByInitializer.Add(javaScriptInitializer, entriesForInitializer);
            }

            entriesForInitializer.Add(new JSComponentInfo(componentType, identifier));
        }

        // This is the DTO that we JSON-serialize and send to an internal function on blazor.*.js.
        internal readonly struct JSComponentInfo
        {
            public readonly string Identifier { get; }
            public readonly JSComponentParameter[] Parameters { get; }

            public JSComponentInfo(Type componentType, string identifier)
            {
                Identifier = identifier;

                var parameterTypes = JSComponentInterop.GetComponentParameters(componentType).ParameterTypes;
                Parameters = new JSComponentParameter[parameterTypes.Count];
                var index = 0;
                foreach (var (name, type) in parameterTypes)
                {
                    Parameters[index++] = new JSComponentParameter(name, type);
                }
            }
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
                _ => "object"
            };
        }
    }
}
