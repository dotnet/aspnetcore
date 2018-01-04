// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Identity.Service
{
    public class TokenResult
    {
        public TokenResult(Token token, string serializedValue)
        {
            Token = token;
            SerializedValue = serializedValue;
        }

        public TokenResult(Token token, string serializedValue, string tokenType)
            : this(token, serializedValue)
        {
            Token = token;
            TokenType = tokenType;
            SerializedValue = serializedValue;
        }

        public Token Token { get; }
        public string TokenType { get; }
        public string SerializedValue { get; }
    }
}
