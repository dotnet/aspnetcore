// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Cbor;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// A variation of <see cref="CborReader"/> that is used to read COSE keys in a CTAP2 canonical CBOR encoding form.
/// </summary>
internal sealed class Ctap2CborReader : CborReader
{
    private int _remainingKeys;
    private int? _lastReadLabel;

    public static Ctap2CborReader Create(ReadOnlyMemory<byte> data)
    {
        var reader = new Ctap2CborReader(data);
        if (reader.ReadStartMap() is not { } keyCount)
        {
            throw new CborContentException("CTAP2 canonical CBOR encoding form requires there to be a definite number of keys.");
        }
        reader._remainingKeys = keyCount;
        return reader;
    }

    private Ctap2CborReader(ReadOnlyMemory<byte> data)
        : base(data, CborConformanceMode.Ctap2Canonical)
    {
    }

    public bool TryReadCoseKeyLabel(int expectedLabel)
    {
        // The 'expectedLabel' parameter can hold a label that
        // was read when handling a previous optional field.
        // We only need to read the next label if uninhabited.
        if (_lastReadLabel is null)
        {
            // Check that we have not reached the end of the COSE key object.
            if (_remainingKeys == 0)
            {
                return false;
            }

            _lastReadLabel = ReadInt32();
        }

        if (expectedLabel != _lastReadLabel.Value)
        {
            return false;
        }

        // Read was successful - vacate '_lastReadLabel' to advance reads.
        _lastReadLabel = null;
        _remainingKeys--;
        return true;
    }

    public void ReadCoseKeyLabel(int expectedLabel)
    {
        if (!TryReadCoseKeyLabel(expectedLabel))
        {
            throw new CborContentException("Unexpected COSE key label");
        }
    }
}
