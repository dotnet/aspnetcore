using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator
{
    internal partial class CallbackRegistrationGenerator
    {
        public class Parser
        {
            private readonly GeneratorExecutionContext _context;

            public Parser(GeneratorExecutionContext context)
            {
                _context = context;
            }

            public SourceGenerationSpec Parse(List<MemberAccessExpressionSyntax> syntaxList)
            {
                var sourceGenerationSpec = new SourceGenerationSpec();
                var compilation = _context.Compilation;

                var providerSymbols = new Dictionary<string, (ITypeSymbol, MemberAccessExpressionSyntax)>();
                var iHubConnectionType =
                    compilation.GetTypeByMetadataName("Microsoft.AspNetCore.SignalR.Client.IHubConnection");

                // Go thru candidates and filter
                foreach(var memberAccess in syntaxList)
                {
                    var expressionModel = _context.Compilation.GetSemanticModel(memberAccess.Expression.SyntaxTree);
                    var typeInfo = expressionModel.GetTypeInfo(memberAccess.Expression);

                    // Filter based on receiver symbol
                    if(!SymbolEqualityComparer.Default.Equals(typeInfo.Type, iHubConnectionType) &&
                       (typeInfo.Type.AllInterfaces.IsDefaultOrEmpty ||
                        !typeInfo.Type.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, iHubConnectionType))))
                    {
                        // Member access is not acting on IHubConnection or a type implementing it; as such we will skip
                        continue;
                    }

                    // Extract type symbol
                    ITypeSymbol symbol;
                    if (memberAccess.Name is GenericNameSyntax {Arity: 1} gns)
                    {
                        // Method is using generic syntax so the sole generic arg is the type
                        var argType = gns.TypeArgumentList.Arguments[0];
                        var argModel = _context.Compilation.GetSemanticModel(argType.SyntaxTree);
                        symbol = (ITypeSymbol) argModel.GetSymbolInfo(argType).Symbol;
                    }
                    else if (memberAccess.Name is not GenericNameSyntax
                             && memberAccess.Parent.ChildNodes().FirstOrDefault(x => x is ArgumentListSyntax) is ArgumentListSyntax
                             { Arguments: {Count: 1} } als)
                    {
                        // Method isn't using generic syntax so inspect first expression in arguments to deduce the type
                        var argModel = _context.Compilation.GetSemanticModel(als.Arguments[0].Expression.SyntaxTree);
                        var argTypeInfo = argModel.GetTypeInfo(als.Arguments[0].Expression);
                        symbol = argTypeInfo.Type;
                    }
                    else
                    {
                        // If we are here then candidate has different number of args than we expect so we skip
                        continue;
                    }

                    // Receiver is a IHubConnection, so save argument symbol for generation
                    providerSymbols[symbol.Name] = (symbol, memberAccess);
                }

                // Generate spec for each provider
                foreach (var (providerSymbol, memberAccess) in providerSymbols.Values)
                {
                    var typeSpec = new TypeSpec();
                    typeSpec.TypeName = providerSymbol.Name;
                    typeSpec.FullyQualifiedTypeName = providerSymbol.ToString();
                    typeSpec.CallSite = memberAccess.GetLocation();

                    var members = providerSymbol.GetMembers()
                        .Where(member => member.Kind == SymbolKind.Method)
                        .Select(member => (IMethodSymbol) member)
                        .Union<IMethodSymbol>(providerSymbol.AllInterfaces.SelectMany(x => x
                            .GetMembers()
                            .Where(member => member.Kind == SymbolKind.Method)
                            .Select(member => (IMethodSymbol) member)), SymbolEqualityComparer.Default).ToList();

                    // Generate spec for each method
                    foreach (var member in members)
                    {
                        var methodSpec = new MethodSpec
                        {
                            Name = member.Name
                        };

                        // Validate return type
                        if (!member.ReturnsVoid)
                        {
                            methodSpec.Support = SupportClassification.UnsupportedReturnType;
                            methodSpec.SupportHint = "Only void return type is supported";
                        }

                        // Generate spec for each argument
                        foreach (var parameter in member.Parameters)
                        {
                            var argumentSpec = new ArgumentSpec
                            {
                                Name = parameter.Name, FullyQualifiedTypeName = parameter.Type.ToString()
                            };

                            methodSpec.Arguments.Add(argumentSpec);
                        }

                        typeSpec.Methods.Add(methodSpec);
                    }

                    sourceGenerationSpec.Types.Add(typeSpec);
                }

                return sourceGenerationSpec;
            }
        }
    }
}
