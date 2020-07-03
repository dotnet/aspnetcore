// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Css.Parser.Parser;
using Microsoft.Css.Parser.TreeItems.Selectors;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class RewriteCssCommand : CommandBase
    {
        public RewriteCssCommand(Application parent)
            : base(parent, "rewritecss")
        {
            Sources = Option("-s", "Files to rewrite", CommandOptionType.MultipleValue);
            Outputs = Option("-o", "Output file paths", CommandOptionType.MultipleValue);
            CssScopes = Option("-c", "CSS scope identifiers", CommandOptionType.MultipleValue);
        }

        public CommandOption Sources { get; }

        public CommandOption Outputs { get; }

        public CommandOption CssScopes { get; }

        protected override bool ValidateArguments()
        {
            if (Sources.Values.Count != Outputs.Values.Count)
            {
                Error.WriteLine($"{Sources.Description} has {Sources.Values.Count}, but {Outputs.Description} has {Outputs.Values.Count} values.");
                return false;
            }

            if (Sources.Values.Count != CssScopes.Values.Count)
            {
                Error.WriteLine($"{Sources.Description} has {Sources.Values.Count}, but {CssScopes.Description} has {CssScopes.Values.Count} values.");
                return false;
            }

            return true;
        }

        protected override Task<int> ExecuteCoreAsync()
        {
            Parallel.For(0, Sources.Values.Count, i =>
            {
                var source = Sources.Values[i];
                var output = Outputs.Values[i];
                var cssScope = CssScopes.Values[i];

                var inputText = File.ReadAllText(source);
                var rewrittenCss = AddScopeToSelectors(inputText, cssScope);
                File.WriteAllText(output, rewrittenCss);
            });

            return Task.FromResult(ExitCodeSuccess);
        }

        // Public for tests
        public static string AddScopeToSelectors(string inputText, string cssScope)
        {
            var cssParser = new DefaultParserFactory().CreateParser();
            var stylesheet = cssParser.Parse(inputText, insertComments: false);

            var resultBuilder = new StringBuilder();
            var previousInsertionPosition = 0;

            foreach (var currentInsertionPosition in GetScopeInsertionPositions(stylesheet))
            {
                resultBuilder.Append(inputText.Substring(previousInsertionPosition, currentInsertionPosition - previousInsertionPosition));
                resultBuilder.AppendFormat("[{0}]", cssScope);
                previousInsertionPosition = currentInsertionPosition;
            }

            resultBuilder.Append(inputText.Substring(previousInsertionPosition));

            return resultBuilder.ToString();
        }

        private static IEnumerable<int> GetScopeInsertionPositions(ComplexItem container)
        {
            foreach (var child in container.Children)
            {
                switch (child)
                {
                    case Selector selector:
                        // For a ruleset like ".first child, .second { ... }", we'll see two selectors:
                        //   ".first child," containing two simple selectors: ".first" and "child"
                        //   ".second", containing one simple selector: ".second"
                        // Our goal is to insert immediately after the final simple selector within each selector
                        var lastSimpleSelector = selector.Children.OfType<SimpleSelector>().LastOrDefault();
                        if (lastSimpleSelector != null)
                        {
                            yield return lastSimpleSelector.AfterEnd;
                        }
                        break;

                    case ComplexItem complexItem:
                        // We need these in order, so perform a depth-first search
                        foreach (var afterEnd in GetScopeInsertionPositions(complexItem))
                        {
                            yield return afterEnd;
                        }
                        break;
                }
            }
        }
    }
}
