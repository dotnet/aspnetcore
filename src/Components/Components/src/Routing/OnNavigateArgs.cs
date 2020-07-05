using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Routing
{
    public class OnNavigateArgs
    {
        public OnNavigateArgs(string path, CancellationTokenSource cancellationTokenSource)
        {
            Path = path;
            CancellationTokenSource = cancellationTokenSource;
        }

        public string Path { get; private set; }

        public CancellationTokenSource CancellationTokenSource { get; private set; }
    }
}
