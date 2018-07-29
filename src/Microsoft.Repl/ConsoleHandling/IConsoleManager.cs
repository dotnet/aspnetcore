using System;
using System.Threading;

namespace Microsoft.Repl.ConsoleHandling
{
    public interface IConsoleManager : IWritable
    {
        Point Caret { get; }

        Point CommandStart { get; }

        int CaretPosition { get; }

        IWritable Error { get; }

        bool IsKeyAvailable { get; }

        void Clear();

        void MoveCaret(int positions);

        ConsoleKeyInfo ReadKey(CancellationToken cancellationToken);

        void ResetCommandStart();

        IDisposable AddBreakHandler(Action onBreak);
    }
}
