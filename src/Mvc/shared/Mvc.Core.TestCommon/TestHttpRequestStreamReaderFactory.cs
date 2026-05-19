// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Mvc;

public class TestHttpRequestStreamReaderFactory : IHttpRequestStreamReaderFactory
{
    public TextReader CreateReader(Stream stream, Encoding encoding)
    {
        return new HttpRequestStreamReader(stream, encoding);
    }
}
