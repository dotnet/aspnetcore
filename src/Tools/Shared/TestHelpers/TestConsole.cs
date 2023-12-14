// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Tools.Internal;

public class TestConsole : IConsole
{
    private event ConsoleCancelEventHandler _cancelKeyPress = default!;
    private readonly TaskCompletionSource _cancelKeySubscribed = new TaskCompletionSource();
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
            _cancelKeySubscribed.TrySetResult();
        }
        remove => _cancelKeyPress -= value;
    }

    public Task CancelKeyPressSubscribed => _cancelKeySubscribed.Task;

    public TextWriter Error { get; }
    public TextWriter Out { get; }
    public TextReader In { get; set; } = new StringReader(string.Empty);
    public bool IsInputRedirected { get; set; }
    public bool IsOutputRedirected { get; }
    public bool IsErrorRedirected { get; }
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

    private sealed class TestOutputWriter : TextWriter
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
