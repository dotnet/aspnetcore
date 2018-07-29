using System;

namespace Microsoft.Repl.Commanding
{
    public interface ICommandHistory
    {
        string GetPreviousCommand();

        string GetNextCommand();

        void AddCommand(string command);

        IDisposable SuspendHistory();
    }
}
