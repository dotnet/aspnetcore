using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    public class HeaderPropagationState
    {
        private static AsyncLocal<Dictionary<string, StringValues>> _headers { get; } = new AsyncLocal<Dictionary<string, StringValues>>();

        public Dictionary<string, StringValues> Headers
        {
            get
            {
                return _headers.Value ?? (_headers.Value = new Dictionary<string, StringValues>());
            }
        }
    }
}
