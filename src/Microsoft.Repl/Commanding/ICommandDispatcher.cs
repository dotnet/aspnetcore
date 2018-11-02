// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Repl.Parsing;

namespace Microsoft.Repl.Commanding
{
    public interface ICommandDispatcher
    {
        IParser Parser { get; }

        IReadOnlyList<string> CollectSuggestions(IShellState shellState);

        void OnReady(IShellState shellState);

        Task ExecuteCommandAsync(IShellState shellState, CancellationToken cancellationToken);
    }

    public interface ICommandDispatcher<in TProgramState, in TParseResult> : ICommandDispatcher
        where TParseResult : ICoreParseResult
    {
        IEnumerable<ICommand<TProgramState, TParseResult>> Commands { get; }
    }
}
