using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Repl.Scripting
{
    public interface IScriptExecutor
    {
        Task ExecuteScriptAsync(IShellState shellState, IEnumerable<string> commandTexts, CancellationToken cancellationToken);
    }
}
