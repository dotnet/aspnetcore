// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Repl.Parsing;

namespace Microsoft.Repl.Commanding
{
    public interface ICommand<in TProgramState, in TParseResult>
        where TParseResult : ICoreParseResult
    {
        string GetHelpSummary(IShellState shellState, TProgramState programState);

        string GetHelpDetails(IShellState shellState, TProgramState programState, TParseResult parseResult);

        IEnumerable<string> Suggest(IShellState shellState, TProgramState programState, TParseResult parseResult);

        bool? CanHandle(IShellState shellState, TProgramState programState, TParseResult parseResult);

        Task ExecuteAsync(IShellState shellState, TProgramState programState, TParseResult parseResult, CancellationToken cancellationToken);
    }
}
