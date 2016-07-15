using System;

namespace Microsoft.AspNetCore.NodeServices.Util
{
    public class ConsoleNodeInstanceOutputLogger : INodeInstanceOutputLogger
    {
        public void LogOutputData(string outputData)
        {
            Console.WriteLine("[Node] " + outputData);
        }

        public void LogErrorData(string errorData)
        {
            Console.WriteLine("[Node] " + errorData);
        }
    }
}
