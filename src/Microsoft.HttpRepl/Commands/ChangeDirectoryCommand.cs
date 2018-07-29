using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.HttpRepl.Suggestions;
using Microsoft.Repl;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.Parsing;

namespace Microsoft.HttpRepl.Commands
{
    public class ChangeDirectoryCommand : CommandWithStructuredInputBase<HttpState, ICoreParseResult>
    {
        protected override Task ExecuteAsync(IShellState shellState, HttpState programState, DefaultCommandInput<ICoreParseResult> commandInput, ICoreParseResult parseResult, CancellationToken cancellationToken)
        {
            if (commandInput.Arguments.Count == 0 || string.IsNullOrEmpty(commandInput.Arguments[0]?.Text))
            {
                shellState.ConsoleManager.WriteLine($"/{string.Join("/", programState.PathSections.Reverse())}");
            }
            else
            {
                string[] parts = commandInput.Arguments[0].Text.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                if (commandInput.Arguments[0].Text.StartsWith("/", StringComparison.Ordinal))
                {
                    programState.PathSections.Clear();
                }

                foreach (string part in parts)
                {
                    switch (part)
                    {
                        case ".":
                            break;
                        case "..":
                            if (programState.PathSections.Count > 0)
                            {
                                programState.PathSections.Pop();
                            }
                            break;
                        default:
                            programState.PathSections.Push(part);
                            break;
                    }
                }

                shellState.ConsoleManager.WriteLine($"/{string.Join("/", programState.PathSections.Reverse())}");
            }

            return Task.CompletedTask;
        }

        protected override CommandInputSpecification InputSpec { get; } = CommandInputSpecification.Create("cd")
            .MaximumArgCount(1)
            .Finish();

        protected override string GetHelpDetails(IShellState shellState, HttpState programState, DefaultCommandInput<ICoreParseResult> commandInput, ICoreParseResult parseResult)
        {
            if (commandInput.Arguments.Count == 1 && !string.IsNullOrEmpty(commandInput.Arguments[0]?.Text))
            {
                return "Prints the current directory if no argument is specified, otherwise changes to the specified directory";
            }

            return "Changes to the directory " + commandInput.Arguments[0].Text;
        }

        public override string GetHelpSummary(IShellState shellState, HttpState programState)
        {
            return "cd [directory name] - Prints the current directory if no argument is specified, otherwise changes to the specified directory";
        }

        protected override IEnumerable<string> GetArgumentSuggestionsForText(IShellState shellState, HttpState programState, ICoreParseResult parseResult, DefaultCommandInput<ICoreParseResult> commandInput, string normalCompletionString)
        {
            return ServerPathCompletion.GetCompletions(programState, normalCompletionString);
        }
    }
}
