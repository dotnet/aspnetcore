using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Repl;
using Microsoft.Repl.Commanding;
using Microsoft.Repl.Parsing;

namespace Microsoft.HttpRepl.Commands
{
    public class ListCommand : CommandWithStructuredInputBase<HttpState, ICoreParseResult>
    {
        private const string RecursiveOption = nameof(RecursiveOption);

        protected override Task ExecuteAsync(IShellState shellState, HttpState programState, DefaultCommandInput<ICoreParseResult> commandInput, ICoreParseResult parseResult, CancellationToken cancellationToken)
        {
            if (programState.Structure == null || programState.BaseAddress == null)
            {
                return Task.CompletedTask;
            }

            string path = commandInput.Arguments.Count > 0 ? commandInput.Arguments[0].Text : string.Empty;

            //If it's an absolute URI, nothing to suggest
            if (Uri.TryCreate(path, UriKind.Absolute, out Uri _))
            {
                return Task.CompletedTask;
            }

            IDirectoryStructure s = programState.Structure.TraverseTo(programState.PathSections.Reverse()).TraverseTo(path);

            List<TreeNode> roots = new List<TreeNode>();
            Formatter formatter = new Formatter();

            roots.Add(new TreeNode(formatter, ".", string.Empty));

            if (s.Parent != null)
            {
                roots.Add(new TreeNode(formatter, "..", string.Empty));
            }

            int recursionDepth = 1;

            if (commandInput.Options[RecursiveOption].Count > 0)
            {
                if (string.IsNullOrEmpty(commandInput.Options[RecursiveOption][0]?.Text))
                {
                    recursionDepth = int.MaxValue;
                }
                else if (int.TryParse(commandInput.Options[RecursiveOption][0].Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rd) && rd > 1)
                {
                    recursionDepth = rd;
                }
            }

            foreach (string child in s.DirectoryNames)
            {
                IDirectoryStructure dir = s.GetChildDirectory(child);

                string methods = dir.RequestInfo != null && dir.RequestInfo.Methods.Count > 0 
                    ? "[" + string.Join("|", dir.RequestInfo.Methods) + "]" 
                    : "[]";

                TreeNode dirNode = new TreeNode(formatter, child, methods);
                roots.Add(dirNode);
                Recurse(dirNode, dir, recursionDepth - 1);
            }

            foreach (TreeNode node in roots)
            {
                shellState.ConsoleManager.WriteLine(node.ToString());
            }

            return Task.CompletedTask;
        }

        private static void Recurse(TreeNode parentNode, IDirectoryStructure parent, int remainingDepth)
        {
            if (remainingDepth <= 0)
            {
                return;
            }

            foreach (string child in parent.DirectoryNames)
            {
                IDirectoryStructure dir = parent.GetChildDirectory(child);

                string methods = dir.RequestInfo != null && dir.RequestInfo.Methods.Count > 0 
                    ? "[" + string.Join("|", dir.RequestInfo.Methods) + "]" 
                    : "[]";

                TreeNode node = parentNode.AddChild(child, methods);
                Recurse(node, dir, remainingDepth - 1);
            }
        }



        protected override CommandInputSpecification InputSpec { get; } = CommandInputSpecification.Create("ls")
            .MaximumArgCount(1)
            .WithOption(new CommandOptionSpecification(RecursiveOption, maximumOccurrences: 1, acceptsValue: true, forms: new[] {"-r", "--recursive"}))
            .Finish();

        protected override string GetHelpDetails(IShellState shellState, HttpState programState, DefaultCommandInput<ICoreParseResult> commandInput, ICoreParseResult parseResult)
        {
            return "Lists the contents of a directory";
        }

        public override string GetHelpSummary(IShellState shellState, HttpState programState)
        {
            return "ls - Performs a directory listing";
        }

        protected override IEnumerable<string> GetArgumentSuggestionsForText(IShellState shellState, HttpState programState, ICoreParseResult parseResult, DefaultCommandInput<ICoreParseResult> commandInput, string normalCompletionString)
        {
            if (programState.Structure == null || programState.BaseAddress == null)
            {
                return null;
            }

            //If it's an absolute URI, nothing to suggest
            if (Uri.TryCreate(normalCompletionString, UriKind.Absolute, out Uri _))
            {
                return null;
            }

            string path = normalCompletionString.Replace('\\', '/');
            int searchFrom = normalCompletionString.Length - 1;
            int lastSlash = path.LastIndexOf('/', searchFrom);
            string prefix;

            if (lastSlash < 0)
            {
                path = string.Empty;
                prefix = normalCompletionString;
            }
            else
            {
                path = path.Substring(0, lastSlash + 1);
                prefix = normalCompletionString.Substring(lastSlash + 1);
            }

            IDirectoryStructure s = programState.Structure.TraverseTo(programState.PathSections.Reverse()).TraverseTo(path);

            List<string> results = new List<string>();

            foreach (string child in s.DirectoryNames)
            {
                if (child.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(path + child);
                }
            }

            return results;
        }
    }
}
