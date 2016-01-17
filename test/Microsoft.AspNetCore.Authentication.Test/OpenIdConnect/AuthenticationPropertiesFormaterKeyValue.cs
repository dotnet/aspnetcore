// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http.Authentication;

namespace Microsoft.AspNetCore.Authentication.Tests.OpenIdConnect
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

            var sb = new StringBuilder();
            foreach(var item in data.Items)
            {
                sb.Append(Uri.EscapeDataString(item.Key) + " " + Uri.EscapeDataString(item.Value) + " ");
            }

            return sb.ToString();
        }
        public string Protect(AuthenticationProperties data, string purpose)
        {
            return Protect(data);
        }

        public AuthenticationProperties Unprotect(string protectedText)
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

        public AuthenticationProperties Unprotect(string protectedText, string purpose)
        {
            return Unprotect(protectedText);
        }
    }
}
