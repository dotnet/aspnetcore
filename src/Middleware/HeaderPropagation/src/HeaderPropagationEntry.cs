using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    public class HeaderPropagationEntry
    {
        public string InputName { get; set; }
        public string OutputName { get; set; }
        public StringValues DefaultValues { get; set; }
        public Func<HttpContext, StringValues> DefaultValuesGenerator { get; set; }
        public bool AlwaysAdd { get; set; }
    }
}
