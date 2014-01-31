using System.Collections.Generic;

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
