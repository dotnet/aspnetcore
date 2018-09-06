using System;

namespace Microsoft.Extensions.ApiDescription.Client.Commands
{
    [Serializable]
    public class GetDocumentCommandContext
    {
        public string AssemblyDirectory { get; set; }

        public string AssemblyName { get; set; }

        public string AssemblyPath { get; set; }

        public string DocumentName { get; set; }

        public string Method { get; set; }

        public string Output { get; set; }

        public string Service { get; set; }

        public string Uri { get; set; }
    }
}
