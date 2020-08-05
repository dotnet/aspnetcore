// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal class SharedMemoryFileListEntryStream : FileListEntryStream
    {
        private static readonly Type? MonoWebAssemblyJSRuntimeType
            = Type.GetType("Mono.WebAssembly.Interop.MonoWebAssemblyJSRuntime, Mono.WebAssembly.Interop");

        private static MethodInfo? _cachedInvokeUnmarshalledMethodInfo;

        public SharedMemoryFileListEntryStream(IJSRuntime jsRuntime, ElementReference inputFileElement, FileListEntry file)
            : base(jsRuntime, inputFileElement, file)
        {
            if (MonoWebAssemblyJSRuntimeType == null)
            {
                throw new InvalidOperationException($"Could not resolve the type for the Mono WebAssembly JavaScript runtime.");
            }

            if (!MonoWebAssemblyJSRuntimeType.IsAssignableFrom(jsRuntime.GetType()))
            {
                throw new InvalidOperationException($"Constructor argument '{nameof(jsRuntime)}' must be assignable to type 'MonoWebAssemblyJSRuntime'.");
            }
        }

        protected override async Task<int> CopyFileDataIntoBuffer(long sourceOffset, byte[] destination, int destinationOffset, int maxBytes, CancellationToken cancellationToken)
        {
            await JSRuntime.InvokeVoidAsync(InputFileInterop.EnsureArrayBufferReadyForSharedMemoryInterop, cancellationToken, InputFileElement, File.Id);

            var invokeUnmarshalledMethodInfo = GetCachedInvokeUnmarshalledMethodInfo();

            return (int)invokeUnmarshalledMethodInfo.Invoke(JSRuntime, new object[]
            {
                InputFileInterop.ReadFileDataSharedMemory,
                new ReadRequest
                {
                    InputFileElementReferenceId = InputFileElement.Id,
                    FileId = File.Id,
                    SourceOffset = sourceOffset,
                    Destination = destination,
                    DestinationOffset = destinationOffset,
                    MaxBytes = maxBytes
                }
            })!;
        }

        // TODO: Is it better to reference M.JSInterop.WebAssembly to avoid using reflection?
        // The method call invocation could also be reduced if we cached via Delegate.CreateDelgate().

        private static MethodInfo GetCachedInvokeUnmarshalledMethodInfo()
        {
            Debug.Assert(MonoWebAssemblyJSRuntimeType != null);

            if (_cachedInvokeUnmarshalledMethodInfo == null)
            {
                foreach (var methodInfo in MonoWebAssemblyJSRuntimeType.GetMethods())
                {
                    if (methodInfo.Name == "InvokeUnmarshalled" && methodInfo.GetParameters().Length == 2)
                    {
                        _cachedInvokeUnmarshalledMethodInfo = methodInfo.MakeGenericMethod(typeof(ReadRequest), typeof(int));
                        break;
                    }
                }

                if (_cachedInvokeUnmarshalledMethodInfo == null)
                {
                    throw new InvalidOperationException("Could not find the 2-parameter overload of 'InvokeUnmarshalled'.");
                }
            }

            return _cachedInvokeUnmarshalledMethodInfo;
        }
    }
}
