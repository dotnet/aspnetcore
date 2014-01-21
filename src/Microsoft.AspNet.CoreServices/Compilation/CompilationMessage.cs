using System;

namespace Microsoft.AspNet.CoreServices
{
    public class CompilationMessage
    {
        public CompilationMessage(string message)
        {
            Message = message;
        }

        public string Message {get; private set;}

        public override string ToString()
        {
            return Message;
        }
    }
}
