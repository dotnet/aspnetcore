using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.Helpers
{
    internal static class SymbolExtensions
    {
        /// <summary>
        /// Check if a property is an auto-property.
        /// TODO: Remove this helper when https://github.com/dotnet/roslyn/issues/46682 is handled.
        /// </summary>
        public static bool IsAutoProperty(this IPropertySymbol propertySymbol)
            => propertySymbol.ContainingType.GetMembers().OfType<IFieldSymbol>().Any(f => f.IsImplicitlyDeclared && SymbolEqualityComparer.Default.Equals(propertySymbol, f.AssociatedSymbol));

        /// <summary>
        /// Returns a value indicating whether the specified symbol has the specified
        /// attribute.
        /// </summary>
        /// <param name="symbol">
        /// The symbol being examined.
        /// </param>
        /// <param name="attribute">
        /// The attribute in question.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="symbol"/> has an attribute of type
        /// <paramref name="attribute"/>; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// If <paramref name="symbol"/> is a type, this method does not find attributes
        /// on its base types.
        /// </remarks>
        public static bool HasAttribute(this ISymbol symbol, [NotNullWhen(returnValue: true)] INamedTypeSymbol? attribute)
        {
            return attribute != null && symbol.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attribute));
        }


    }
}
