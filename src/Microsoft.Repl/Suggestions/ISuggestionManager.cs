namespace Microsoft.Repl.Suggestions
{
    public interface ISuggestionManager
    {
        void NextSuggestion(IShellState shellState);

        void PreviousSuggestion(IShellState shellState);
    }
}
