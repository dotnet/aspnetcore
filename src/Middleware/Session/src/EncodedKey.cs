// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Session;

// Keys are stored in their utf-8 encoded state.
// This saves us from de-serializing and re-serializing every key on every request.
internal sealed class EncodedKey
{
    private string? _keyString;
    private int? _hashCode;

    internal EncodedKey(string key)
    {
        _keyString = key;
        KeyBytes = Encoding.UTF8.GetBytes(key);
    }

    public EncodedKey(byte[] key)
    {
        KeyBytes = key;
    }

    internal string KeyString
    {
        get
        {
            if (_keyString == null)
            {
                _keyString = Encoding.UTF8.GetString(KeyBytes, 0, KeyBytes.Length);
            }
            return _keyString;
        }
    }

    internal byte[] KeyBytes { get; private set; }

    public override bool Equals(object? obj)
    {
        var otherKey = obj as EncodedKey;
        if (otherKey == null)
        {
            return false;
        }
        if (KeyBytes.Length != otherKey.KeyBytes.Length)
        {
            return false;
        }
        if (_hashCode.HasValue && otherKey._hashCode.HasValue
            && _hashCode.Value != otherKey._hashCode.Value)
        {
            return false;
        }
        for (int i = 0; i < KeyBytes.Length; i++)
        {
            if (KeyBytes[i] != otherKey.KeyBytes[i])
            {
                return false;
            }
        }
        return true;
    }

    public override int GetHashCode()
    {
        if (!_hashCode.HasValue)
        {
            _hashCode = SipHash.GetHashCode(KeyBytes);
        }
        return _hashCode.Value;
    }

    public override string ToString()
    {
        return KeyString;
    }
}
