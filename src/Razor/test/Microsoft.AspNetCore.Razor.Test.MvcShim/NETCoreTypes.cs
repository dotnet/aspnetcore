// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETFRAMEWORK

// Razor's tests cross-compile to verify that it works on .NET Framework. Unfortunately they compile
// against types in the framework BCL which means types exclusive to .NET Core would result in compilation
// errors.
// Fixing this would require re-authoring tests to always include .NET Core ref assemblies, but that
// is a fairly tedious processes. Until this becomes a broader problem, we'll shim new framework types
// that need to be referenced in tests.

namespace System.Runtime.CompilerServices;

/// <summary>
/// Indicates a type should be replaced rather than updated when applying metadata updates.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class CreateNewOnMetadataUpdateAttribute : Attribute
{
}
#endif
