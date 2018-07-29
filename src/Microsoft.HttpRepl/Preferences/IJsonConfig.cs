using Microsoft.Repl.ConsoleHandling;

namespace Microsoft.HttpRepl.Preferences
{
    public interface IJsonConfig
    {
        int IndentSize { get; }

        AllowedColors DefaultColor { get; }

        AllowedColors ArrayBraceColor { get; }

        AllowedColors ObjectBraceColor { get; }

        AllowedColors CommaColor { get; }

        AllowedColors NameColor { get; }

        AllowedColors NameSeparatorColor { get; }

        AllowedColors BoolColor { get; }

        AllowedColors NumericColor { get; }

        AllowedColors StringColor { get; }

        AllowedColors NullColor { get; }
    }
}