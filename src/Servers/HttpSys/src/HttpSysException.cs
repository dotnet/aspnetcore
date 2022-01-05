// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.HttpSys;

/// <summary>
/// Exception thrown by HttpSys when an error occurs
/// </summary>
[SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
public class HttpSysException : Win32Exception
{
    internal HttpSysException()
        : base(Marshal.GetLastWin32Error())
    {
    }

    internal HttpSysException(int errorCode)
        : base(errorCode)
    {
    }

    internal HttpSysException(int errorCode, string message)
        : base(errorCode, message)
    {
    }

    // the base class returns the HResult with this property
    // we need the Win32 Error Code, hence the override.
    /// <inheritdoc />
    public override int ErrorCode
    {
        get
        {
            return NativeErrorCode;
        }
    }
}
