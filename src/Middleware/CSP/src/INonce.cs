// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;

namespace Microsoft.AspNetCore.Csp
{
    /// <summary>
    /// Simple interface representing a CSP nonce value.
    /// </summary>
    public interface INonce
    {
        string GetValue();
    }

    /// <summary>
    /// Default nonce implementation. Computes a random nonce and returns its base64 value on instantiation.
    /// </summary>
    public class Nonce : INonce
    {
        private readonly string _value;
        private static readonly Lazy<Random> _gen = new Lazy<Random>(() => new Random());

        public Nonce()
        {
            byte[] bytes = new byte[7];
            _gen.Value.NextBytes(bytes);
            _value = Convert.ToBase64String(bytes);
        }

        public string GetValue()
        {
            return _value;
        }
    }
}
