using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class CodeBuilderResult
    {
        public CodeBuilderResult(string code, IList<LineMapping> designTimeLineMappings)
        {
            Code = code;
            DesignTimeLineMappings = designTimeLineMappings;
        }

        public string Code { get; private set; }
        public IList<LineMapping> DesignTimeLineMappings { get; private set; }
    }
}
