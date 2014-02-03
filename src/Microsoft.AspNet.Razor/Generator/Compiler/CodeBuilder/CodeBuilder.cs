
namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public abstract class CodeBuilder
    {
        private readonly CodeGeneratorContext _context;

        public CodeBuilder(CodeGeneratorContext context)
        {
            _context = context;
        }

        protected CodeGeneratorContext Context
        {
            get { return _context; }
        }

        public abstract CodeBuilderResult Build();
    }
}
