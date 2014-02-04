using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpCodeBuilder : CodeBuilder
    {
        public CSharpCodeBuilder(CodeGeneratorContext context)
            : base(context)
        {
        }

        private CodeTree Tree { get { return Context.CodeTreeBuilder.CodeTree; } }
        public RazorEngineHost Host { get { return Context.Host; } }

        public override CodeBuilderResult Build()
        {
            var writer = new CSharpCodeWriter();

            using (writer.BuildNamespace(Context.RootNamespace))
            {
                // Write out using directives
                AddImports(Tree, writer, Host.NamespaceImports);
                // Separate the usings and the class
                writer.WriteLine();

                var baseTypeVisitor = new CSharpBaseTypeVisitor(writer, Context);
                baseTypeVisitor.Accept(Tree.Chunks);

                string baseType = baseTypeVisitor.CurrentBaseType ?? Host.DefaultBaseClass;
                new CSharpClassAttributeVisitor(writer, Context).Accept(Tree.Chunks);

                IEnumerable<string> baseTypes = String.IsNullOrEmpty(baseType) ? Enumerable.Empty<string>() :
                                                                                 new string[] { baseType };
                using (writer.BuildClassDeclaration("public", Context.ClassName, baseTypes))
                {
                    if (Host.DesignTimeMode)
                    {
                        writer.WriteLine("private static object @__o;");
                    }

                    new CSharpHelperVisitor(writer, Context).Accept(Tree.Chunks);
                    new CSharpTypeMemberVisitor(writer, Context).Accept(Tree.Chunks);
                    new CSharpDesignTimeHelpersVisitor(writer, Context).AcceptTree(Tree);
                  
                    writer.WriteLineHiddenDirective();
                    using (writer.BuildConstructor(Context.ClassName))
                    {
                        // Any constructor based logic that we need to add?
                    };

                    // Add space inbetween constructor and method body
                    writer.WriteLine();

                    using (writer.BuildMethodDeclaration("public override", "void", Host.GeneratedClassContext.ExecuteMethodName))
                    {
                        new CSharpCodeVisitor(writer, Context).Accept(Tree.Chunks);
                    }
                }
            }

            return new CodeBuilderResult(writer.ToString(), writer.LineMappingManager.Mappings);
        }

        private void AddImports(CodeTree codeTree, CSharpCodeWriter writer, IEnumerable<string> defaultImports)
        {
            // Write out using directives
            var usingVisitor = new CSharpUsingVisitor(writer, Context);
            foreach (Chunk chunk in Tree.Chunks)
            {
                usingVisitor.Accept(chunk);
            }

            defaultImports = defaultImports.Except(usingVisitor.ImportedUsings);

            foreach (string import in defaultImports)
            {
                writer.WriteUsing(import);
            }
        }
    }
}
