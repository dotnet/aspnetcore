// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

/// <summary>
/// Creates and manages the lifetime of <see cref="T:ViewBufferValue[]"/> instances.
/// </summary>
public interface IViewBufferScope
{
    /// <summary>
    /// Gets a <see cref="T:ViewBufferValue[]"/>.
    /// </summary>
    /// <param name="pageSize">The minimum size of the segment.</param>
    /// <returns>The <see cref="T:ViewBufferValue[]"/>.</returns>
    ViewBufferValue[] GetPage(int pageSize);

    /// <summary>
    /// Returns a <see cref="T:ViewBufferValue[]"/> that can be reused.
    /// </summary>
    /// <param name="segment">The <see cref="T:ViewBufferValue[]"/>.</param>
    void ReturnSegment(ViewBufferValue[] segment);

    /// <summary>
    /// Creates a <see cref="PagedBufferedTextWriter"/> that will delegate to the provided
    /// <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The <see cref="TextWriter"/>.</param>
    /// <returns>A <see cref="PagedBufferedTextWriter"/>.</returns>
    TextWriter CreateWriter(TextWriter writer);
}
