namespace Microsoft.Repl.Commanding
{
    public enum CommandInputProcessingIssueKind
    {
        CommandMismatch,
        ArgumentCountOutOfRange,
        UnknownOption,
        OptionUseCountOutOfRange,
        MissingRequiredOptionInput,
    }
}