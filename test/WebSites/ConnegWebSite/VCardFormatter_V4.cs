// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ConnegWebSite.Models;
using Microsoft.AspNet.Mvc;
using Microsoft.Net.Http.Headers;

namespace ConnegWebSite
{
    /// <summary>
    /// Provides contact information of a person through VCard format.
    /// In version 4.0 of VCard format, Gender is a supported property.
    /// </summary>
    public class VCardFormatter_V4 : OutputFormatter
    {
        public VCardFormatter_V4()
        {
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/vcard;version=v4.0"));
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
            builder.AppendFormat("GENDER:{0}", (contact.Gender == GenderType.Male) ? "M" : "F");
            builder.AppendLine();
            builder.AppendLine("END:VCARD");

            var responseStream = new DelegatingStream(context.ActionContext.HttpContext.Response.Body);
            using (var writer = new StreamWriter(responseStream, context.SelectedEncoding, bufferSize: 1024))
            {
                await writer.WriteAsync(builder.ToString());
            }
        }
    }
}