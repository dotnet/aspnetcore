// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace RepoTasks
{
    /// <summary>
    /// See https://confluence.jetbrains.com/display/TCD10/Build+Script+Interaction+with+TeamCity
    /// </summary>
    internal class TeamCityMessageWriter : IWriter
    {
        private const string MessagePrefix = "##teamcity";
        private static readonly string EOL = Environment.NewLine;
        private readonly WriteHandler _write;
        private readonly string _flowIdAttr;

        public TeamCityMessageWriter(WriteHandler write, string flowId)
        {
            _write = write;
            _flowIdAttr = EscapeTeamCityText(flowId);

            WriteHandler = CreateMessageHandler();
        }

        public WriteHandler WriteHandler { get; }

        public void OnBuildStarted(BuildStartedEventArgs e)
        {
            _write($"##teamcity[blockOpened name='Build {_flowIdAttr}' flowId='{_flowIdAttr}']" + EOL);
        }

        public void OnBuildFinished(BuildFinishedEventArgs e)
        {
            _write($"##teamcity[blockClosed name='Build {_flowIdAttr}' flowId='{_flowIdAttr}']" + EOL);
        }

        private WriteHandler CreateMessageHandler()
        {
            var format = "##teamcity[message text='{0}' flowId='" + _flowIdAttr + "']" + EOL;
            return message =>
            {
                if (string.IsNullOrEmpty(message))
                {
                    return;
                }

                if (message.StartsWith(MessagePrefix, StringComparison.Ordinal))
                {
                    _write(message);
                    return;
                }

                _write(
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        format,
                        EscapeTeamCityText(message)));
            };
        }

        private static string EscapeTeamCityText(string txt)
        {
            if (string.IsNullOrEmpty(txt))
            {
                return txt;
            }

            var sb = new StringBuilder(txt.Length);
            for (var i = 0; i < txt.Length; i++)
            {
                var ch = txt[i];
                switch (ch)
                {
                    case '\'':
                    case '|':
                    case '[':
                    case ']':
                        sb.Append('|').Append(ch);
                        break;
                    case '\n':
                        sb.Append("|n");
                        break;
                    case '\r':
                        sb.Append("|r");
                        break;
                    case '\u0085':
                        sb.Append("|x");
                        break;
                    case '\u2028':
                        sb.Append("|l");
                        break;
                    case '\u2029':
                        sb.Append("|p");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
