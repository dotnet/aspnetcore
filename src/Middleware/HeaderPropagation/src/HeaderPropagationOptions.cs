using System.Collections.Generic;

namespace Microsoft.Extensions.Http.HeaderPropagation
{
    public class HeaderPropagationOptions
    {
        public IList<HeaderPropagationEntry> Headers { get; set; } = new List<HeaderPropagationEntry>();
    }
}
