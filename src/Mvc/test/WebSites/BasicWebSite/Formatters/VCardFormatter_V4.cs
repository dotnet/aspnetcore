// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using System.Text;
using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace BasicWebSite.Formatters;

/// <summary>
/// Provides contact information of a person through VCard format.
/// In version 4.0 of VCard format, Gender is a supported property.
/// </summary>
public class VCardFormatter_V4 : TextOutputFormatter
{
    public VCardFormatter_V4()
    {
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/vcard;version=v4.0"));
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
        builder.AppendFormat(CultureInfo.InvariantCulture, "FN:{0}", contact.Name);
        builder.AppendLine();
        builder.AppendFormat(CultureInfo.InvariantCulture, "GENDER:{0}", (contact.Gender == GenderType.Male) ? "M" : "F");
        builder.AppendLine();
        builder.AppendLine("END:VCARD");

        await context.HttpContext.Response.WriteAsync(
            builder.ToString(),
            selectedEncoding);
    }
}
