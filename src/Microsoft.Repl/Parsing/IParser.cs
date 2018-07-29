namespace Microsoft.Repl.Parsing
{
    public interface IParser
    {
        ICoreParseResult Parse(string commandText, int caretPosition);
    }

    public interface IParser<out TParseResult> : IParser
    {
        new TParseResult Parse(string commandText, int caretPosition);
    }
}
