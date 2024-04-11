// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Runtime.CompilerServices;

#if !NET5_0_OR_GREATER
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit { } // for "init" support on down-level TFMs
#endif
