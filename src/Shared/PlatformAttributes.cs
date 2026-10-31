// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copied from https://raw.githubusercontent.com/dotnet/runtime/b45ee9d37afec0c88141053e86ccf71c6f283000/src/libraries/System.Private.CoreLib/src/System/Runtime/Versioning/PlatformAttributes.cs

#nullable enable
namespace System.Runtime.Versioning;

/// <summary>
/// Base type for all platform-specific API attributes.
/// </summary>
#pragma warning disable CS3015 // Type has no accessible constructors which use only CLS-compliant types
#if SYSTEM_PRIVATE_CORELIB
    public
#else
internal
#endif
        abstract class OSPlatformAttribute : Attribute
#pragma warning restore CS3015
{
    private protected OSPlatformAttribute(string platformName)
    {
        PlatformName = platformName;
    }
    public string PlatformName { get; }
}

/// <summary>
/// Records the platform that the project targeted.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly,
                AllowMultiple = false, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
internal
#endif
        sealed class TargetPlatformAttribute : OSPlatformAttribute
{
    public TargetPlatformAttribute(string platformName) : base(platformName)
    {
    }
}

/// <summary>
/// Records the operating system (and minimum version) that supports an API. Multiple attributes can be
/// applied to indicate support on multiple operating systems.
/// </summary>
/// <remarks>
/// Callers can apply a <see cref="System.Runtime.Versioning.SupportedOSPlatformAttribute " />
/// or use guards to prevent calls to APIs on unsupported operating systems.
///
/// A given platform should only be specified once.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly |
                AttributeTargets.Class |
                AttributeTargets.Constructor |
                AttributeTargets.Enum |
                AttributeTargets.Event |
                AttributeTargets.Field |
                AttributeTargets.Method |
                AttributeTargets.Module |
                AttributeTargets.Property |
                AttributeTargets.Struct,
                AllowMultiple = true, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
internal
#endif
        sealed class SupportedOSPlatformAttribute : OSPlatformAttribute
{
    public SupportedOSPlatformAttribute(string platformName) : base(platformName)
    {
    }
}

/// <summary>
/// Marks APIs that were removed in a given operating system version.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly |
                AttributeTargets.Class |
                AttributeTargets.Constructor |
                AttributeTargets.Enum |
                AttributeTargets.Event |
                AttributeTargets.Field |
                AttributeTargets.Method |
                AttributeTargets.Module |
                AttributeTargets.Property |
                AttributeTargets.Struct,
                AllowMultiple = true, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
internal
#endif
        sealed class UnsupportedOSPlatformAttribute : OSPlatformAttribute
{
    public UnsupportedOSPlatformAttribute(string platformName) : base(platformName)
    {
    }
}
