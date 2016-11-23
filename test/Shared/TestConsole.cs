// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Tools.Internal
{
    public class TestConsole : IConsole
    {
        public TestConsole(ITestOutputHelper output)
        {
            var writer = new TestOutputWriter(output);
            Error = writer;
            Out = writer;
        }

        public event ConsoleCancelEventHandler CancelKeyPress;

        public TextWriter Error { get; set; }
        public TextWriter Out { get; set; }
        public TextReader In { get; set; } = new StringReader(string.Empty);
        public bool IsInputRedirected { get; set; } = false;
        public bool IsOutputRedirected { get; } = false;
        public bool IsErrorRedirected { get; } = false;

        public ConsoleCancelEventArgs ConsoleCancelKey()
        {
            var ctor = typeof(ConsoleCancelEventArgs)
                .GetTypeInfo()
                .DeclaredConstructors
                .Single(c => c.GetParameters().First().ParameterType == typeof(ConsoleSpecialKey));
            var args = (ConsoleCancelEventArgs)ctor.Invoke(new object[] { ConsoleSpecialKey.ControlC });
            CancelKeyPress?.Invoke(this, args);
            return args;
        }

        private class TestOutputWriter : TextWriter
        {
            private readonly ITestOutputHelper _output;
            private readonly StringBuilder _sb = new StringBuilder();

            public TestOutputWriter(ITestOutputHelper output)
            {
                _output = output;
            }

            public override Encoding Encoding => Encoding.Unicode;

            public override void Write(char value)
            {
                if (value == '\r' || value == '\n')
                {
                    if (_sb.Length > 0)
                    {
                        _output.WriteLine(_sb.ToString());
                        _sb.Clear();
                    }
                }
                else
                {
                    _sb.Append(value);
                }
            }
        }
    }
}
