// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Text;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging.Testing.Tests;

public class TestTestOutputHelper : ITestOutputHelper
{
    private StringBuilder _output = new StringBuilder();

    public bool Throw { get; set; }

    public string Output => _output.ToString();

    public void WriteLine(string message)
    {
        if (Throw)
        {
            throw new Exception("Boom!");
        }
        _output.AppendLine(message);
    }

    public void WriteLine(string format, params object[] args)
    {
        if (Throw)
        {
            throw new Exception("Boom!");
        }
        _output.AppendLine(string.Format(CultureInfo.InvariantCulture, format, args));
    }
}
