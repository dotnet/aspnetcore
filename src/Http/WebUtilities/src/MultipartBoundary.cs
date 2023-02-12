// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.WebUtilities;

internal sealed class MultipartBoundary
{
    private readonly int[] _skipTable = new int[256];
    private readonly string _boundary;
    private bool _expectLeadingCrlf;

    public MultipartBoundary(string boundary, bool expectLeadingCrlf = true)
    {
        ArgumentNullException.ThrowIfNull(boundary);

        _boundary = boundary;
        _expectLeadingCrlf = expectLeadingCrlf;
        Initialize(_boundary, _expectLeadingCrlf);
    }

    private void Initialize(string boundary, bool expectLeadingCrlf)
    {
        if (expectLeadingCrlf)
        {
            BoundaryBytes = Encoding.UTF8.GetBytes("\r\n--" + boundary);
        }
        else
        {
            BoundaryBytes = Encoding.UTF8.GetBytes("--" + boundary);
        }
        FinalBoundaryLength = BoundaryBytes.Length + 2; // Include the final '--' terminator.

        var length = BoundaryBytes.Length;
        for (var i = 0; i < _skipTable.Length; ++i)
        {
            _skipTable[i] = length;
        }
        for (var i = 0; i < length; ++i)
        {
            _skipTable[BoundaryBytes[i]] = Math.Max(1, length - 1 - i);
        }
    }

    public int GetSkipValue(byte input)
    {
        return _skipTable[input];
    }

    public bool ExpectLeadingCrlf
    {
        get { return _expectLeadingCrlf; }
        set
        {
            if (value != _expectLeadingCrlf)
            {
                _expectLeadingCrlf = value;
                Initialize(_boundary, _expectLeadingCrlf);
            }
        }
    }

    public byte[] BoundaryBytes { get; private set; } = default!; // This gets initialized as part of Initialize called from in the ctor.

    public int FinalBoundaryLength { get; private set; }
}
