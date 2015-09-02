using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.StandardsPolice
{
    public class Program
    {
        public int Main(string[] args)
        {
            var tree = CSharpSyntaxTree.ParseText(@"
public class Hello { public Hello(int foo){}; protected int _foo; int _bar; }
public class World { public World(int foo){}; protected int _foo; int _bar; static int _quux = 4; enum Blah{} class Clazz{} }
");
            var diags = new List<Diagnostic>();

            var comp = CSharpCompilation.Create("Comp", new[] { tree });

            StandardsPoliceCompileModule.ScanCompilation(diags, comp);

            var hello = comp.GetTypeByMetadataName("Hello");
            foreach (var f in hello.GetMembers().OfType<IFieldSymbol>())
            {
                var syntax = f.DeclaringSyntaxReferences.Single().GetSyntax();
                Console.WriteLine($"{syntax.ToFullString()}");

                var fds = syntax.Parent.Parent as FieldDeclarationSyntax;
                var toks = syntax.DescendantTokens().ToArray();
                var nods = syntax.DescendantNodesAndSelf().ToArray();
                var mods = fds.Modifiers;

                foreach (var mod in fds.Modifiers)
                {
                    Console.WriteLine($"{mod.Kind()} {mod.ToFullString()}");
                }
                var locs = f.Locations.ToArray();
            }

            foreach(var d in diags)
            {
                Console.WriteLine(d);
            }
            return 0;
        }
    }
}
