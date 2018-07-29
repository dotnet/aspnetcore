using System.Threading;
using System.Threading.Tasks;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.Input;
using Microsoft.Repl.Suggestions;

namespace Microsoft.Repl
{
    public class Shell
    {
        public Shell(IShellState shellState)
        {
            KeyHandlers.RegisterDefaultKeyHandlers(shellState.InputManager);
            ShellState = shellState;
        }

        public Shell(ICommandDispatcher dispatcher, ISuggestionManager suggestionManager = null)
            : this(new ShellState(dispatcher, suggestionManager))
        {
        }

        public IShellState ShellState { get; }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            ShellState.CommandDispatcher.OnReady(ShellState);
            return ShellState.InputManager.StartAsync(ShellState, cancellationToken);
        }
    }
}
