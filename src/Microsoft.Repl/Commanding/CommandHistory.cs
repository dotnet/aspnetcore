// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Repl.Commanding
{
    public class CommandHistory : ICommandHistory
    {
        private readonly int _maxEntries;
        private readonly List<string> _commandLines = new List<string>();
        private int _currentCommand = -1;
        private int _suspensionDepth;

        public CommandHistory(int maxEntries = 50)
        {
            _maxEntries = maxEntries;
        }

        public void AddCommand(string command)
        {
            if (_suspensionDepth > 0)
            {
                return;
            }

            _commandLines.Add(command);
            if (_commandLines.Count > _maxEntries)
            {
                _commandLines.RemoveAt(0);
            }
            _currentCommand = -1;
        }

        public string GetNextCommand()
        {
            if (_commandLines.Count == 0)
            {
                return string.Empty;
            }

            if (_currentCommand == -1 || _currentCommand >= _commandLines.Count - 1)
            {
                _currentCommand = -1;
                return string.Empty;
            }

            return _commandLines[++_currentCommand];
        }

        public string GetPreviousCommand()
        {
            if (_commandLines.Count == 0)
            {
                return string.Empty;
            }

            if (_currentCommand == -1)
            {
                _currentCommand = _commandLines.Count;
            }

            if (_currentCommand > 0)
            {
                return _commandLines[--_currentCommand];
            }

            return _commandLines[0];
        }

        public IDisposable SuspendHistory()
        {
            ++_suspensionDepth;
            return new Disposable(() => --_suspensionDepth);
        }
    }
}
