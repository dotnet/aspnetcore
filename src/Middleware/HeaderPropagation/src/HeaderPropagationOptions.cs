using System.Collections.Generic;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    public class HeaderPropagationOptions
    {
        public IList<HeaderPropagationEntry> Headers { get; set; } = new List<HeaderPropagationEntry>();
    }
}
