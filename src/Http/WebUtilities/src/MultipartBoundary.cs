// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.WebUtilities;

internal sealed class MultipartBoundary
{
    private readonly byte[] _boundaryBytes;
    private bool _expectLeadingCrlf;

    public MultipartBoundary(string boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);

        _expectLeadingCrlf = false;
        _boundaryBytes = Encoding.UTF8.GetBytes("\r\n--" + boundary);

        FinalBoundaryLength = BoundaryBytes.Length + 2; // Include the final '--' terminator.
    }

    public void ExpectLeadingCrlf()
    {
        _expectLeadingCrlf = true;
    }

    // Lets us throw a more specific error from MultipartReaderStream when reading any preamble data.
    public bool BeforeFirstBoundary()
    {
        return !_expectLeadingCrlf;
    }

    // Return either "--{boundary}" or "\r\n--{boundary}" depending on if we're looking for the end of a section
    public ReadOnlySpan<byte> BoundaryBytes => _boundaryBytes.AsSpan(_expectLeadingCrlf ? 0 : 2);

    public int FinalBoundaryLength { get; private set; }
}
