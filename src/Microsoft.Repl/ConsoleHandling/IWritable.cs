namespace Microsoft.Repl.ConsoleHandling
{
    public interface IWritable
    {
        void Write(char c);

        void Write(string s);

        void WriteLine();

        void WriteLine(string s);

        bool IsCaretVisible { get; set; }
    }
}
