// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// An interface that can provide data protection services.
/// Is an optimized version of <see cref="IDataProtector"/>.
/// </summary>
public interface ISpanDataProtector : IDataProtector
{
    void Protect<TWriter>(ReadOnlySpan<byte> plaintext, TWriter destination)
        where TWriter : IBufferWriter<byte>
#if NET
        , allows ref struct
#endif
        ;

    void Unprotect<TWriter>(ReadOnlySpan<byte> protectedData, TWriter destination)
        where TWriter : IBufferWriter<byte>
#if NET
        , allows ref struct
#endif
        ;
}
