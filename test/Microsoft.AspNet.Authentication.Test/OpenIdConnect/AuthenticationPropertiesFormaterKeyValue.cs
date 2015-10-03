// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.Extensions.WebEncoders;

namespace Microsoft.AspNet.Authentication.Tests.OpenIdConnect
{
    /// <summary>
    /// This formatter creates an easy to read string of the format: "'key1' 'value1' ..."
    /// </summary>
    public class AuthenticationPropertiesFormaterKeyValue : ISecureDataFormat<AuthenticationProperties>
    {
        string _protectedString = Guid.NewGuid().ToString();

        public string Protect(AuthenticationProperties data)
        {
            if (data == null || data.Items.Count == 0)
            {
                return "null";
            }

            var encoder = UrlEncoder.Default;
            var sb = new StringBuilder();
            foreach(var item in data.Items)
            {
                sb.Append(encoder.UrlEncode(item.Key) + " " + encoder.UrlEncode(item.Value) + " ");
            }

            return sb.ToString();
        }

        AuthenticationProperties ISecureDataFormat<AuthenticationProperties>.Unprotect(string protectedText)
        {
            if (string.IsNullOrEmpty(protectedText))
            {
                return null;
            }

            if (protectedText == "null")
            {
                return new AuthenticationProperties();
            }

            string[] items = protectedText.Split(' ');
            if (items.Length % 2 != 0)
            {
                return null;
            }

            var propeties = new AuthenticationProperties();
            for (int i = 0; i < items.Length - 1; i+=2)
            {
                propeties.Items.Add(items[i], items[i + 1]);
            }

            return propeties;
        }
    }
}
