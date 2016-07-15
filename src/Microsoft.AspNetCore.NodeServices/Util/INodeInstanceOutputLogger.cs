using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices.Util
{
    public interface INodeInstanceOutputLogger
    {
        void LogOutputData(string outputData);

        void LogErrorData(string errorData);
    }
}
