// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ContentNegotiationWebSite.Models;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.Net.Http.Headers;

namespace ContentNegotiationWebSite
{
    /// <summary>
    /// Provides contact information of a person through VCard format.
    /// </summary>
    public class VCardFormatter_V3 : OutputFormatter
    {
        public VCardFormatter_V3()
        {
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/vcard;version=v3.0"));
        }

        protected override bool CanWriteType(Type declaredType, Type runtimeType)
        {
            return typeof(Contact).GetTypeInfo().IsAssignableFrom(runtimeType.GetTypeInfo());
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterContext context)
        {
            var contact = (Contact)context.Object;

            var builder = new StringBuilder();
            builder.AppendLine("BEGIN:VCARD");
            builder.AppendFormat("FN:{0}", contact.Name);
            builder.AppendLine();
            builder.AppendLine("END:VCARD");

            await context.HttpContext.Response.WriteAsync(builder.ToString(), context.SelectedEncoding);
        }
    }
}