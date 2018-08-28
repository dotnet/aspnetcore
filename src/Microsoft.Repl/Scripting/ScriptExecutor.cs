// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.Parsing;

namespace Microsoft.Repl.Scripting
{
    public class ScriptExecutor<TProgramState, TParseResult> : IScriptExecutor
        where TParseResult : ICoreParseResult
    {
        private readonly bool _hideScriptLinesFromHistory;

        public ScriptExecutor(bool hideScriptLinesFromHistory = true)
        {
            _hideScriptLinesFromHistory = hideScriptLinesFromHistory;
        }

        public async Task ExecuteScriptAsync(IShellState shellState, IEnumerable<string> commandTexts, CancellationToken cancellationToken)
        {
            if (shellState.CommandDispatcher is ICommandDispatcher<TProgramState, TParseResult> dispatcher)
            {
                IDisposable suppressor = _hideScriptLinesFromHistory ? shellState.CommandHistory.SuspendHistory() : null;

                using (suppressor)
                {
                    foreach (string commandText in commandTexts)
                    {
                        if (string.IsNullOrWhiteSpace(commandText))
                        {
                            continue;
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        dispatcher.OnReady(shellState);
                        shellState.ConsoleManager.ResetCommandStart();
                        shellState.InputManager.SetInput(shellState, commandText);
                        await dispatcher.ExecuteCommandAsync(shellState, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
