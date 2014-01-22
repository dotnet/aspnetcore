using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class CompilationFailedException : Exception
    {
        public CompilationFailedException(IEnumerable<CompilationMessage> messages, string generatedCode)
            : base(FormatMessage(messages))
        {
            Messages = messages.ToList();
            GeneratedCode = generatedCode;
        }

        public string GeneratedCode { get; private set; }

        public IEnumerable<CompilationMessage> Messages { get; private set; }

        public string CompilationSource
        {
            get { return GeneratedCode; }
        }

        public override string Message
        {
            get
            {
                return "Compilation Failed:" + FormatMessage(Messages);
            }
        }

        private static string FormatMessage(IEnumerable<CompilationMessage> messages)
        {
            return String.Join(Environment.NewLine, messages);
        }
    }
}
