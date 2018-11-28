// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BasicWebSite.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace BasicWebSite.Formatters
{
    /// <summary>
    /// Provides contact information of a person through VCard format.
    /// </summary>
    public class VCardFormatter_V3 : TextOutputFormatter
    {
        public VCardFormatter_V3()
        {
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/vcard;version=v3.0"));
        }

        protected override bool CanWriteType(Type type)
        {
            return typeof(Contact).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var contact = (Contact)context.Object;

            var builder = new StringBuilder();
            builder.AppendLine("BEGIN:VCARD");
            builder.AppendFormat("FN:{0}", contact.Name);
            builder.AppendLine();
            builder.AppendLine("END:VCARD");

            await context.HttpContext.Response.WriteAsync(
                builder.ToString(),
                selectedEncoding);
        }
    }
}