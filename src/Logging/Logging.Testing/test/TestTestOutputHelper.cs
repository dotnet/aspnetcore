// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging.Testing.Tests
{
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
            _output.AppendLine(string.Format(format, args));
        }
    }
}
