// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Repl.Commanding;
using Microsoft.Repl.ConsoleHandling;
using Microsoft.Repl.Input;
using Microsoft.Repl.Suggestions;

namespace Microsoft.Repl
{
    public class ShellState : IShellState
    {
        public ShellState(ICommandDispatcher commandDispatcher, ISuggestionManager suggestionManager = null, IInputManager inputManager = null, ICommandHistory commandHistory = null, IConsoleManager consoleManager = null)
        {
            InputManager = inputManager ?? new InputManager();
            CommandHistory = commandHistory ?? new CommandHistory();
            ConsoleManager = consoleManager ?? new ConsoleManager();
            CommandDispatcher = commandDispatcher;
            SuggestionManager = suggestionManager ?? new SuggestionManager();
        }

        public IInputManager InputManager { get; }

        public ICommandHistory CommandHistory { get; }

        public IConsoleManager ConsoleManager { get; }

        public ICommandDispatcher CommandDispatcher { get; }

        public bool IsExiting { get; set; }

        public ISuggestionManager SuggestionManager { get; }
    }
}
