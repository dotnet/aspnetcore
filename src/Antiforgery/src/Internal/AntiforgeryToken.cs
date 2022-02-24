// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery;

internal sealed class AntiforgeryToken
{
    internal const int SecurityTokenBitLength = 128;
    internal const int ClaimUidBitLength = 256;

    private string _additionalData = string.Empty;
    private string _username = string.Empty;
    private BinaryBlob? _securityToken;

    public string AdditionalData
    {
        get { return _additionalData; }
        set
        {
            _additionalData = value ?? string.Empty;
        }
    }

    public BinaryBlob? ClaimUid { get; set; }

    public bool IsCookieToken { get; set; }

    public BinaryBlob? SecurityToken
    {
        get
        {
            if (_securityToken == null)
            {
                _securityToken = new BinaryBlob(SecurityTokenBitLength);
            }
            return _securityToken;
        }
        set
        {
            _securityToken = value;
        }
    }

    public string? Username
    {
        get { return _username; }
        set
        {
            _username = value ?? string.Empty;
        }
    }
}
