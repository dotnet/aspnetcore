// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Tools.Internal;
using Xunit;

namespace Microsoft.Extensions.Tools.Tests
{
    public class ReporterTests
    {
        private static readonly string EOL = Environment.NewLine;

        [Theory]
        [InlineData(ConsoleColor.DarkGray, "\x1B[90m")]
        [InlineData(ConsoleColor.Red, "\x1B[91m")]
        [InlineData(ConsoleColor.Yellow, "\x1B[93m")]
        public void WrapsWithAnsiColorCode(ConsoleColor color, string code)
        {
            Assert.Equal($"{code}sample\x1B[39m", new ColorFormatter(color).Format("sample"));
        }

        [Fact]
        public void SkipsColorCodesForEmptyOrNullInput()
        {
            var formatter = new ColorFormatter(ConsoleColor.Blue);
            Assert.Empty(formatter.Format(string.Empty));
            Assert.Null(formatter.Format(null));
        }

        [Fact]
        public void WritesToStandardStreams()
        {
            var testConsole = new TestConsole();
            var reporter = new FormattingReporter(testConsole,
                DefaultFormatter.Instance, DefaultFormatter.Instance,
                DefaultFormatter.Instance, DefaultFormatter.Instance);

            // stdout
            reporter.Verbose("verbose");
            Assert.Equal("verbose" + EOL, testConsole.GetOutput());
            testConsole.Clear();

            reporter.Output("out");
            Assert.Equal("out" + EOL, testConsole.GetOutput());
            testConsole.Clear();

            reporter.Warn("warn");
            Assert.Equal("warn" + EOL, testConsole.GetOutput());
            testConsole.Clear();

            // stderr
            reporter.Error("error");
            Assert.Equal("error" + EOL, testConsole.GetError());
            testConsole.Clear();
        }

        [Fact]
        public void FailsToBuildWithoutConsole()
        {
            Assert.Throws<InvalidOperationException>(
                () => new ReporterBuilder().Build());
        }

        private class TestConsole : IConsole
        {
            private readonly StringBuilder _out;
            private readonly StringBuilder _error;

            public TestConsole()
            {
                _out = new StringBuilder();
                _error = new StringBuilder();
                Out = new StringWriter(_out);
                Error = new StringWriter(_error);
            }

            event ConsoleCancelEventHandler IConsole.CancelKeyPress
            {
                add { }
                remove { }
            }

            public string GetOutput() => _out.ToString();
            public string GetError() => _error.ToString();

            public void Clear()
            {
                _out.Clear();
                _error.Clear();
            }

            public TextWriter Out { get; }
            public TextWriter Error { get; }
            public TextReader In { get; }
            public bool IsInputRedirected { get; }
            public bool IsOutputRedirected { get; }
            public bool IsErrorRedirected { get; }
        }
    }
}