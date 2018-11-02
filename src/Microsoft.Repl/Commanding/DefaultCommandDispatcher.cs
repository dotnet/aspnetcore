// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Repl.ConsoleHandling;
using Microsoft.Repl.Parsing;

namespace Microsoft.Repl.Commanding
{
    public static class DefaultCommandDispatcher
    {
        public static DefaultCommandDispatcher<TProgramState> Create<TProgramState>(Func<string> getPrompt, TProgramState programState)
        {
            return new DefaultCommandDispatcher<TProgramState>(getPrompt, programState);
        }

        public static DefaultCommandDispatcher<TProgramState> Create<TProgramState>(Action<IShellState> onReady, TProgramState programState)
        {
            return new DefaultCommandDispatcher<TProgramState>(onReady, programState);
        }

        public static DefaultCommandDispatcher<TProgramState, TParseResult> Create<TProgramState, TParseResult>(Func<string> getPrompt, TProgramState programState, IParser<TParseResult> parser)
            where TParseResult : ICoreParseResult
        {
            return new DefaultCommandDispatcher<TProgramState, TParseResult>(getPrompt, programState, parser);
        }

        public static DefaultCommandDispatcher<TProgramState, TParseResult> Create<TProgramState, TParseResult>(Action<IShellState> onReady, TProgramState programState, IParser<TParseResult> parser)
            where TParseResult : ICoreParseResult
        {
            return new DefaultCommandDispatcher<TProgramState, TParseResult>(onReady, programState, parser);
        }
    }

    public class DefaultCommandDispatcher<TProgramState> : DefaultCommandDispatcher<TProgramState, ICoreParseResult>
    {
        public DefaultCommandDispatcher(Func<string> getPrompt, TProgramState programState)
            : base(getPrompt, programState, new CoreParser())
        {
        }

        public DefaultCommandDispatcher(Action<IShellState> onReady, TProgramState programState)
            : base(onReady, programState, new CoreParser())
        {
        }
    }

    public class DefaultCommandDispatcher<TProgramState, TParseResult> : ICommandDispatcher<TProgramState, TParseResult>
        where TParseResult : ICoreParseResult
    {
        private readonly Action<IShellState> _onReady;
        private readonly TProgramState _programState;
        private readonly IParser<TParseResult> _parser;
        private readonly HashSet<ICommand<TProgramState, TParseResult>> _commands = new HashSet<ICommand<TProgramState, TParseResult>>();
        private bool _isReady;

        public DefaultCommandDispatcher(Func<string> getPrompt, TProgramState programState, IParser<TParseResult> parser)
            : this(s => s.ConsoleManager.Write(getPrompt()), programState, parser)
        {
        }

        public DefaultCommandDispatcher(Action<IShellState> onReady, TProgramState programState, IParser<TParseResult> parser)
        {
            _onReady = onReady;
            _programState = programState;
            _parser = parser;
        }

        public void AddCommand(ICommand<TProgramState, TParseResult> command)
        {
            _commands.Add(command);
        }

        public IEnumerable<ICommand<TProgramState, TParseResult>> Commands => _commands;

        public IParser Parser => _parser;

        public IReadOnlyList<string> CollectSuggestions(IShellState shellState)
        {
            string line = shellState.InputManager.GetCurrentBuffer();
            TParseResult parseResult = _parser.Parse(line, shellState.ConsoleManager.CaretPosition);
            HashSet<string> suggestions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ICommand<TProgramState, TParseResult> command in _commands)
            {
                IEnumerable<string> commandSuggestions = command.Suggest(shellState, _programState, parseResult);

                if (commandSuggestions != null)
                {
                    suggestions.UnionWith(commandSuggestions);
                }
            }

            return suggestions.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public async Task ExecuteCommandAsync(IShellState shellState, CancellationToken cancellationToken)
        {
            _isReady = false;
            shellState.ConsoleManager.WriteLine();
            string commandText = shellState.InputManager.GetCurrentBuffer();

            if (!string.IsNullOrWhiteSpace(commandText))
            {
                shellState.CommandHistory.AddCommand(shellState.InputManager.GetCurrentBuffer());

                try
                {
                    await ExecuteCommandInternalAsync(shellState, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    shellState.ConsoleManager.Error.WriteLine(ex.ToString().Bold().Red());
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    shellState.ConsoleManager.Error.WriteLine("Execution was cancelled".Bold().Red());
                }
            }

            if (!_isReady)
            {
                shellState.ConsoleManager.WriteLine();
                OnReady(shellState);
            }

            shellState.InputManager.ResetInput();
        }

        private async Task ExecuteCommandInternalAsync(IShellState shellState, CancellationToken cancellationToken)
        {
            string line = shellState.InputManager.GetCurrentBuffer();
            TParseResult parseResult = _parser.Parse(line, shellState.ConsoleManager.CaretPosition);

            if (!string.IsNullOrWhiteSpace(parseResult.CommandText))
            {
                foreach (ICommand<TProgramState, TParseResult> command in _commands)
                {
                    bool? result = command.CanHandle(shellState, _programState, parseResult);

                    if (result.HasValue)
                    {
                        if (result.Value)
                        {
                            await command.ExecuteAsync(shellState, _programState, parseResult, cancellationToken);
                        }

                        //If the handler returned non-null, the input would be directed to it, but it's not valid input
                        return;
                    }
                }

                shellState.ConsoleManager.Error.WriteLine("No matching command found".Red().Bold());
                shellState.ConsoleManager.Error.WriteLine("Execute 'help' to see available commands".Red().Bold());
            }
        }

        public void OnReady(IShellState shellState)
        {
            if (!_isReady)
            {
                _onReady(shellState);
                shellState.ConsoleManager.ResetCommandStart();
                _isReady = true;
            }
        }
    }
}
