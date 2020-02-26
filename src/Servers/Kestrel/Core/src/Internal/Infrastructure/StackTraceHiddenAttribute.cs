// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Diagnostics
{
    /// <summary>
    /// Attribute to add to non-returning throw only methods, 
    /// to restore the stack trace back to what it would be if the throw was in-place
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Struct, Inherited = false)]
    internal sealed class StackTraceHiddenAttribute : Attribute
    {
        // https://github.com/dotnet/coreclr/blob/eb54e48b13fdfb7233b7bcd32b93792ba3e89f0c/src/mscorlib/shared/System/Diagnostics/StackTraceHiddenAttribute.cs
        public StackTraceHiddenAttribute() { }
    }
}