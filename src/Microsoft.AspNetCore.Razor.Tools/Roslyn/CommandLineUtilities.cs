// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;

namespace Roslyn.Utilities
{
    /*
     * Copied from https://github.com/dotnet/roslyn/blob/master/src/Compilers/Core/Portable/InternalUtilities/CommandLineUtilities.cs
     */
    internal static class CommandLineUtilities
    {
        /// <summary>
        /// Split a command line by the same rules as Main would get the commands except the original
        /// state of backslashes and quotes are preserved.  For example in normal Windows command line 
        /// parsing the following command lines would produce equivalent Main arguments:
        /// 
        ///     - /r:a,b
        ///     - /r:"a,b"
        /// 
        /// This method will differ as the latter will have the quotes preserved.  The only case where 
        /// quotes are removed is when the entire argument is surrounded by quotes without any inner
        /// quotes. 
        /// </summary>
        /// <remarks>
        /// Rules for command line parsing, according to MSDN:
        /// 
        /// Arguments are delimited by white space, which is either a space or a tab.
        ///  
        /// A string surrounded by double quotation marks ("string") is interpreted 
        /// as a single argument, regardless of white space contained within. 
        /// A quoted string can be embedded in an argument.
        ///  
        /// A double quotation mark preceded by a backslash (\") is interpreted as a 
        /// literal double quotation mark character (").
        ///  
        /// Backslashes are interpreted literally, unless they immediately precede a 
        /// double quotation mark.
        ///  
        /// If an even number of backslashes is followed by a double quotation mark, 
        /// one backslash is placed in the argv array for every pair of backslashes, 
        /// and the double quotation mark is interpreted as a string delimiter.
        ///  
        /// If an odd number of backslashes is followed by a double quotation mark, 
        /// one backslash is placed in the argv array for every pair of backslashes, 
        /// and the double quotation mark is "escaped" by the remaining backslash, 
        /// causing a literal double quotation mark (") to be placed in argv.
        /// </remarks>
        public static IEnumerable<string> SplitCommandLineIntoArguments(string commandLine, bool removeHashComments)
        {
            char? unused;
            return SplitCommandLineIntoArguments(commandLine, removeHashComments, out unused);
        }

        public static IEnumerable<string> SplitCommandLineIntoArguments(string commandLine, bool removeHashComments, out char? illegalChar)
        {
            var builder = new StringBuilder(commandLine.Length);
            var list = new List<string>();
            var i = 0;

            illegalChar = null;
            while (i < commandLine.Length)
            {
                while (i < commandLine.Length && char.IsWhiteSpace(commandLine[i]))
                {
                    i++;
                }

                if (i == commandLine.Length)
                {
                    break;
                }

                if (commandLine[i] == '#' && removeHashComments)
                {
                    break;
                }

                var quoteCount = 0;
                builder.Length = 0;
                while (i < commandLine.Length && (!char.IsWhiteSpace(commandLine[i]) || (quoteCount % 2 != 0)))
                {
                    var current = commandLine[i];
                    switch (current)
                    {
                        case '\\':
                            {
                                var slashCount = 0;
                                do
                                {
                                    builder.Append(commandLine[i]);
                                    i++;
                                    slashCount++;
                                } while (i < commandLine.Length && commandLine[i] == '\\');

                                // Slashes not followed by a quote character can be ignored for now
                                if (i >= commandLine.Length || commandLine[i] != '"')
                                {
                                    break;
                                }

                                // If there is an odd number of slashes then it is escaping the quote
                                // otherwise it is just a quote.
                                if (slashCount % 2 == 0)
                                {
                                    quoteCount++;
                                }

                                builder.Append('"');
                                i++;
                                break;
                            }

                        case '"':
                            builder.Append(current);
                            quoteCount++;
                            i++;
                            break;

                        default:
                            if ((current >= 0x1 && current <= 0x1f) || current == '|')
                            {
                                if (illegalChar == null)
                                {
                                    illegalChar = current;
                                }
                            }
                            else
                            {
                                builder.Append(current);
                            }

                            i++;
                            break;
                    }
                }

                // If the quote string is surrounded by quotes with no interior quotes then 
                // remove the quotes here. 
                if (quoteCount == 2 && builder[0] == '"' && builder[builder.Length - 1] == '"')
                {
                    builder.Remove(0, length: 1);
                    builder.Remove(builder.Length - 1, length: 1);
                }

                if (builder.Length > 0)
                {
                    list.Add(builder.ToString());
                }
            }

            return list;
        }
    }
}