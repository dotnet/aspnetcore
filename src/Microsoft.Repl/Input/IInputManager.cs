using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Repl.Input
{
    public interface IInputManager
    {
        bool IsOverwriteMode { get; set; }

        IInputManager RegisterKeyHandler(ConsoleKey key, AsyncKeyPressHandler handler);

        void ResetInput();

        Task StartAsync(IShellState state, CancellationToken cancellationToken);

        void SetInput(IShellState state, string input);

        string GetCurrentBuffer();

        void RemovePreviousCharacter(IShellState state);

        void RemoveCurrentCharacter(IShellState state);

        void Clear(IShellState state);
    }
}
