using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class CodeBuilder
    {
        protected CodeTree Tree;

        public CodeBuilder(CodeTree codeTree)
        {
            Tree = codeTree;
        }

        public virtual CodeBuilderResult Build()
        {
            return null;
        }
    }
}
