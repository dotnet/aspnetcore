// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Identity.Service
{
    [DebuggerDisplay("{GetDebugDisplay(),nq}")]
    public class SigningCredentialsDescriptor
    {
        public SigningCredentialsDescriptor(
            SigningCredentials credentials,
            string algorithm,
            DateTimeOffset notBefore,
            DateTimeOffset expires,
            IDictionary<string, string> metadata)
        {
            Credentials = credentials;
            Algorithm = algorithm;
            NotBefore = notBefore;
            Expires = expires;
            Metadata = metadata;
        }

        public string Id => Credentials.Kid;
        public string Algorithm { get; set; }
        public DateTimeOffset NotBefore { get; set; }
        public DateTimeOffset Expires { get; set; }
        public SigningCredentials Credentials { get; set; }
        public IDictionary<string, string> Metadata { get; set; }

        private string GetDebugDisplay()
        {
            var builder = new StringBuilder();
            builder.Append($"Id = {Id}, ");
            builder.Append($"Alg = {Algorithm}, ");
            builder.Append($"Nbf = {NotBefore}, ");
            builder.Append($"Exp = {Expires}, ");
            foreach (var kvp in Metadata)
            {
                builder.Append($"{kvp.Key} = {kvp.Value}, ");
            }

            return builder.ToString();
        }
    }
}
