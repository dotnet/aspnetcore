using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNet.Mvc.Razor
{
    public static class RazorErrorExtensions
    {
        public static Diagnostic ToDiagnostics([NotNull] this RazorError error, [NotNull] string filePath)
        {
            var descriptor = new DiagnosticDescriptor(id: "Razor",
                                                      title: "Razor parsing error",
                                                      messageFormat: error.Message.Replace("{", "{{").Replace("}", "}}"),
                                                      category: "Razor.Parser",
                                                      defaultSeverity: DiagnosticSeverity.Error,
                                                      isEnabledByDefault: true);

            var textSpan = new TextSpan(error.Location.AbsoluteIndex, error.Length);
            var linePositionStart = new LinePosition(error.Location.LineIndex, error.Location.CharacterIndex);
            var linePositionEnd = new LinePosition(error.Location.LineIndex,
                                                   error.Location.CharacterIndex + error.Length);
            var linePositionSpan = new LinePositionSpan(linePositionStart, linePositionEnd);

            var location = Location.Create(filePath, textSpan, linePositionSpan);

            return Diagnostic.Create(descriptor, location);
        }
    }
}