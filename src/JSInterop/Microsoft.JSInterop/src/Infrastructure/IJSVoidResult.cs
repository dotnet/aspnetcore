// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.ComponentModel;

namespace Microsoft.JSInterop.Infrastructure;

/// <summary>
/// Represents a void result from a JavaScript call.
/// This property is public to support cross-assembly accessibility for WebAssembly and should not be used by user code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IJSVoidResult
{
}
