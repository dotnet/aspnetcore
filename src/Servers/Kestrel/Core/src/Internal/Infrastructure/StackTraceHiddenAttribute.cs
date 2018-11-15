// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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