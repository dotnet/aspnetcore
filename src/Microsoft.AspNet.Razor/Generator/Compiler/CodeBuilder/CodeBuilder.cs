
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
