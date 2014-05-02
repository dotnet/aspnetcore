// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

#if DEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor
{
    internal static class RazorDebugHelpers
    {
        private static bool _outputDebuggingEnabled = IsDebuggingEnabled();

        private static readonly Dictionary<char, string> _printableEscapeChars = new Dictionary<char, string>
        {
            { '\0', "\\0" },
            { '\\', "\\\\" },
            { '\'', "'" },
            { '\"', "\\\"" },
            { '\a', "\\a" },
            { '\b', "\\b" },
            { '\f', "\\f" },
            { '\n', "\\n" },
            { '\r', "\\r" },
            { '\t', "\\t" },
            { '\v', "\\v" },
        };

        internal static bool OutputDebuggingEnabled
        {
            get { return _outputDebuggingEnabled; }
        }

        internal static void WriteDebugTree(string sourceFile, Block document, PartialParseResult result, TextChange change, RazorEditorParser parser, bool treeStructureChanged)
        {
            if (!OutputDebuggingEnabled)
            {
                return;
            }

            RunTask(() =>
            {
                string outputFileName = Normalize(sourceFile) + "_tree";
                string outputPath = Path.Combine(Path.GetDirectoryName(sourceFile), outputFileName);

                var treeBuilder = new StringBuilder();
                WriteTree(document, treeBuilder);
                treeBuilder.AppendLine();
                treeBuilder.AppendFormat(CultureInfo.CurrentCulture, "Last Change: {0}", change);
                treeBuilder.AppendLine();
                treeBuilder.AppendFormat(CultureInfo.CurrentCulture, "Normalized To: {0}", change.Normalize());
                treeBuilder.AppendLine();
                treeBuilder.AppendFormat(CultureInfo.CurrentCulture, "Partial Parse Result: {0}", result);
                treeBuilder.AppendLine();
                if (result.HasFlag(PartialParseResult.Rejected))
                {
                    treeBuilder.AppendFormat(CultureInfo.CurrentCulture, "Tree Structure Changed: {0}", treeStructureChanged);
                    treeBuilder.AppendLine();
                }
                if (result.HasFlag(PartialParseResult.AutoCompleteBlock))
                {
                    treeBuilder.AppendFormat(CultureInfo.CurrentCulture, "Auto Complete Insert String: \"{0}\"", parser.GetAutoCompleteString());
                    treeBuilder.AppendLine();
                }
                File.WriteAllText(outputPath, treeBuilder.ToString());
            });
        }

        private static void WriteTree(SyntaxTreeNode node, StringBuilder treeBuilder, int depth = 0)
        {
            if (node == null)
            {
                return;
            }
            if (depth > 1)
            {
                WriteIndent(treeBuilder, depth);
            }

            if (depth > 0)
            {
                treeBuilder.Append("|-- ");
            }

            treeBuilder.AppendLine(ConvertEscapseSequences(node.ToString()));
            if (node.IsBlock)
            {
                foreach (SyntaxTreeNode child in ((Block)node).Children)
                {
                    WriteTree(child, treeBuilder, depth + 1);
                }
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is debug only")]
        [SuppressMessage("Microsoft.Web.FxCop", "MW1202:DoNotUseProblematicTaskTypes", Justification = "This rule is not applicable to this assembly.")]
        [SuppressMessage("Microsoft.Web.FxCop", "MW1201:DoNotCallProblematicMethodsOnTask", Justification = "This rule is not applicable to this assembly.")]
        private static void RunTask(Action action)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    action();
                }
                catch
                {
                    // Catch all errors since this is just a debug helper
                }
            });
        }

        private static void WriteIndent(StringBuilder sb, int depth)
        {
            for (int i = 0; i < (depth - 1) * 4; ++i)
            {
                if (i % 4 == 0)
                {
                    sb.Append("|");
                }
                else
                {
                    sb.Append(" ");
                }
            }
        }

        private static string Normalize(string path)
        {
            return Path.GetFileName(path).Replace('.', '_');
        }

        private static string ConvertEscapseSequences(string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var ch in value)
            {
                sb.Append(GetCharValue(ch));
            }
            return sb.ToString();
        }

        private static string GetCharValue(char ch)
        {
            string value;
            if (_printableEscapeChars.TryGetValue(ch, out value))
            {
                return value;
            }
            return ch.ToString();
        }

        private static bool IsDebuggingEnabled()
        {
            bool enabled;
            return Boolean.TryParse(Environment.GetEnvironmentVariable("RAZOR_DEBUG"), out enabled) && enabled;
        }
    }
}

#endif
