using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.HttpLogging
{
    public sealed class TestW3CLoggerOptions : W3CLoggerOptions
    {
        public int NumWrites { get; set; }
    }
}
