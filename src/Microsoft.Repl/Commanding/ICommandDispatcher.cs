using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Repl.Parsing;

namespace Microsoft.Repl.Commanding
{
    public interface ICommandDispatcher
    {
        IParser Parser { get; }

        IReadOnlyList<string> CollectSuggesetions(IShellState shellState);

        void OnReady(IShellState shellState);

        Task ExecuteCommandAsync(IShellState shellState, CancellationToken cancellationToken);
    }

    public interface ICommandDispatcher<in TProgramState, in TParseResult> : ICommandDispatcher
        where TParseResult : ICoreParseResult
    {
        IEnumerable<ICommand<TProgramState, TParseResult>> Commands { get; }
    }
}
