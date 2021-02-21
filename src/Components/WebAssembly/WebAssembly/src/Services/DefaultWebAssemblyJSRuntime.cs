// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.JSInterop.Infrastructure;
using Microsoft.JSInterop.WebAssembly;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services
{
    internal sealed class DefaultWebAssemblyJSRuntime : WebAssemblyJSRuntime
    {
        internal static readonly DefaultWebAssemblyJSRuntime Instance = new DefaultWebAssemblyJSRuntime();

        public ElementReferenceContext ElementReferenceContext { get; }

        [DynamicDependency(nameof(InvokeDotNet))]
        [DynamicDependency(nameof(EndInvokeJS))]
        [DynamicDependency(nameof(BeginInvokeDotNet))]
        private DefaultWebAssemblyJSRuntime()
        {
            ElementReferenceContext = new WebElementReferenceContext(this);
            JsonSerializerOptions.Converters.Add(new ElementReferenceJsonConverter(ElementReferenceContext));
        }

        // The following methods are invoke via Mono's JS interop mechanism (invoke_method)
        public static string? InvokeDotNet(string assemblyName, string methodIdentifier, string dotNetObjectId, string argsJson)
        {
            var callInfo = new DotNetInvocationInfo(assemblyName, methodIdentifier, dotNetObjectId == null ? default : long.Parse(dotNetObjectId, CultureInfo.InvariantCulture), callId: null);
            return DotNetDispatcher.Invoke(Instance, callInfo, argsJson);
        }

        // Invoked via Mono's JS interop mechanism (invoke_method)
        public static void EndInvokeJS(string argsJson)
            => DotNetDispatcher.EndInvokeJS(Instance, argsJson);

        // Invoked via Mono's JS interop mechanism (invoke_method)
        public static void BeginInvokeDotNet(string callId, string assemblyNameOrDotNetObjectId, string methodIdentifier, string argsJson)
        {
            // Figure out whether 'assemblyNameOrDotNetObjectId' is the assembly name or the instance ID
            // We only need one for any given call. This helps to work around the limitation that we can
            // only pass a maximum of 4 args in a call from JS to Mono WebAssembly.
            string? assemblyName;
            long dotNetObjectId;
            if (char.IsDigit(assemblyNameOrDotNetObjectId[0]))
            {
                dotNetObjectId = long.Parse(assemblyNameOrDotNetObjectId, CultureInfo.InvariantCulture);
                assemblyName = null;
            }
            else
            {
                dotNetObjectId = default;
                assemblyName = assemblyNameOrDotNetObjectId;
            }

            var callInfo = new DotNetInvocationInfo(assemblyName, methodIdentifier, dotNetObjectId, callId);
            DotNetDispatcher.BeginInvokeDotNet(Instance, callInfo, argsJson);
        }
    }
}
