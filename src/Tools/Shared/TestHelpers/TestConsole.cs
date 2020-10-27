// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Text;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Tools.Internal
{
    public class TestConsole : IConsole
    {
        private event ConsoleCancelEventHandler _cancelKeyPress;
        private readonly TaskCompletionSource<bool> _cancelKeySubscribed = new TaskCompletionSource<bool>();
        private readonly TestOutputWriter _testWriter;

        public TestConsole(ITestOutputHelper output)
        {
            _testWriter = new TestOutputWriter(output);
            Error = _testWriter;
            Out = _testWriter;
        }

        public event ConsoleCancelEventHandler CancelKeyPress
        {
            add
            {
                _cancelKeyPress += value;
                _cancelKeySubscribed.TrySetResult(true);
            }
            remove => _cancelKeyPress -= value;
        }

        public Task CancelKeyPressSubscribed => _cancelKeySubscribed.Task;

        public TextWriter Error { get; }
        public TextWriter Out { get; }
        public TextReader In { get; set; } = new StringReader(string.Empty);
        public bool IsInputRedirected { get; set; } = false;
        public bool IsOutputRedirected { get; } = false;
        public bool IsErrorRedirected { get; } = false;
        public ConsoleColor ForegroundColor { get; set; }

        public ConsoleCancelEventArgs ConsoleCancelKey()
        {
            var ctor = typeof(ConsoleCancelEventArgs)
                .GetTypeInfo()
                .DeclaredConstructors
                .Single(c => c.GetParameters().First().ParameterType == typeof(ConsoleSpecialKey));
            var args = (ConsoleCancelEventArgs)ctor.Invoke(new object[] { ConsoleSpecialKey.ControlC });
            _cancelKeyPress.Invoke(this, args);
            return args;
        }

        public void ResetColor()
        {
        }

        public string GetOutput()
        {
            return _testWriter.GetOutput();
        }

        public void ClearOutput()
        {
            _testWriter.ClearOutput();
        }

        private class TestOutputWriter : TextWriter
        {
            private readonly ITestOutputHelper _output;
            private readonly StringBuilder _sb = new StringBuilder();
            private readonly StringBuilder _currentOutput = new StringBuilder();

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

                    _currentOutput.Append(value);
                }
                else
                {
                    _sb.Append(value);
                    _currentOutput.Append(value);
                }
            }

            public string GetOutput()
            {
                return _currentOutput.ToString();
            }

            public void ClearOutput()
            {
                _currentOutput.Clear();
            }
        }
    }
}
