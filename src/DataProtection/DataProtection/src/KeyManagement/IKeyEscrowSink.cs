// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

/// <summary>
/// The basic interface for implementing a key escrow sink.
/// </summary>
/// <remarks>
/// <see cref="IKeyEscrowSink"/> is distinct from <see cref="IXmlRepository"/> in that
/// <see cref="IKeyEscrowSink"/> provides a write-only interface and instances handle unencrypted key material,
/// while <see cref="IXmlRepository"/> provides a read+write interface and instances handle encrypted key material.
/// </remarks>
public interface IKeyEscrowSink
{
    /// <summary>
    /// Stores the given key material to the escrow service.
    /// </summary>
    /// <param name="keyId">The id of the key being persisted to escrow.</param>
    /// <param name="element">The unencrypted key material in XML form.</param>
    void Store(Guid keyId, XElement element);
}
