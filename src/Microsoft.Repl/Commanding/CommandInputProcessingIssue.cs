namespace Microsoft.Repl.Commanding
{
    public class CommandInputProcessingIssue
    {
        public CommandInputProcessingIssueKind Kind { get; }

        public string Text { get; }

        public CommandInputProcessingIssue(CommandInputProcessingIssueKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }
    }
}