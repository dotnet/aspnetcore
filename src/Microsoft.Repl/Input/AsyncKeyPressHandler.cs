using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Repl.Input
{
    public delegate Task AsyncKeyPressHandler(ConsoleKeyInfo keyInfo, IShellState state, CancellationToken cancellationToken);
}
