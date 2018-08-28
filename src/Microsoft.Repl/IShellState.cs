// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Repl.Commanding;
using Microsoft.Repl.ConsoleHandling;
using Microsoft.Repl.Input;
using Microsoft.Repl.Suggestions;

namespace Microsoft.Repl
{
    public interface IShellState
    {
        IInputManager InputManager { get; }

        ICommandHistory CommandHistory { get; }

        IConsoleManager ConsoleManager { get; }

        ICommandDispatcher CommandDispatcher { get; }

        ISuggestionManager SuggestionManager { get; }

        bool IsExiting { get; set; }
    }
}
