// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// this controls if the logs are written to the console.
// they can be reviewed for general content.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Authentication.Tests.OpenIdConnect
{
    public class LoggingUtilities
    {
        static List<LogEntry> CompleteLogEntries;
        static Dictionary<string, LogLevel> LogEntries;

        static LoggingUtilities()
        {
            LogEntries =
                new Dictionary<string, LogLevel>()
                {
                    { "OIDCH_0000:", LogLevel.Debug },
                    { "OIDCH_0001:", LogLevel.Debug },
                    { "OIDCH_0002:", LogLevel.Verbose },
                    { "OIDCH_0003:", LogLevel.Verbose },
                    { "OIDCH_0004:", LogLevel.Warning },
                    { "OIDCH_0005:", LogLevel.Error },
                    { "OIDCH_0006:", LogLevel.Error },
                    { "OIDCH_0007:", LogLevel.Verbose },
                    { "OIDCH_0008:", LogLevel.Verbose },
                    { "OIDCH_0009:", LogLevel.Verbose },
                    { "OIDCH_0010:", LogLevel.Error },
                    { "OIDCH_0011:", LogLevel.Error },
                    { "OIDCH_0012:", LogLevel.Verbose },
                    { "OIDCH_0013:", LogLevel.Verbose },
                    { "OIDCH_0014:", LogLevel.Debug },
                    { "OIDCH_0015:", LogLevel.Verbose },
                    { "OIDCH_0016:", LogLevel.Verbose },
                    { "OIDCH_0017:", LogLevel.Error },
                    { "OIDCH_0018:", LogLevel.Verbose },
                    { "OIDCH_0019:", LogLevel.Verbose },
                    { "OIDCH_0020:", LogLevel.Debug },
                    { "OIDCH_0021:", LogLevel.Verbose },
                    { "OIDCH_0026:", LogLevel.Error },
                    { "OIDCH_0038:", LogLevel.Debug },
                    { "OIDCH_0040:", LogLevel.Debug },
                    { "OIDCH_0042:", LogLevel.Debug },
                    { "OIDCH_0043:", LogLevel.Verbose },
                    { "OIDCH_0044:", LogLevel.Verbose },
                    { "OIDCH_0045:", LogLevel.Debug }
            };

            BuildLogEntryList();
        }

        /// <summary>
        /// Builds the complete list of OpenIdConnect log entries that are available in the runtime.
        /// </summary>
        private static void BuildLogEntryList()
        {
            CompleteLogEntries = new List<LogEntry>();
            foreach (var entry in LogEntries)
            {
                CompleteLogEntries.Add(new LogEntry { State = entry.Key, Level = entry.Value });
            }
        }

        /// <summary>
        /// Adds to errors if a variation if any are found.
        /// </summary>
        /// <param name="variation">if this has been seen before, errors will be appended, test results are easier to understand if this is unique.</param>
        /// <param name="capturedLogs">these are the logs the runtime generated</param>
        /// <param name="expectedLogs">these are the errors that were expected</param>
        /// <param name="errors">the dictionary to record any errors</param>
        public static void CheckLogs(List<LogEntry> capturedLogs, List<LogEntry> expectedLogs, List<Tuple<LogEntry, LogEntry>> errors)
        {
            if (capturedLogs.Count >= expectedLogs.Count)
            {
                for (int i = 0; i < capturedLogs.Count; i++)
                {
                    if (i + 1 > expectedLogs.Count)
                    {
                        errors.Add(new Tuple<LogEntry, LogEntry>(capturedLogs[i], null));
                    }
                    else
                    {
                        if (!TestUtilities.AreEqual<LogEntry>(capturedLogs[i], expectedLogs[i]))
                        {
                            errors.Add(new Tuple<LogEntry, LogEntry>(capturedLogs[i], expectedLogs[i]));
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < expectedLogs.Count; i++)
                {
                    if (i + 1 > capturedLogs.Count)
                    {
                        errors.Add(new Tuple<LogEntry, LogEntry>(null, expectedLogs[i]));
                    }
                    else
                    {
                        if (!TestUtilities.AreEqual<LogEntry>(expectedLogs[i], capturedLogs[i]))
                        {
                            errors.Add(new Tuple<LogEntry, LogEntry>(capturedLogs[i], expectedLogs[i]));
                        }
                    }
                }
            }
        }

        public static string LoggingErrors(List<Tuple<LogEntry, LogEntry>> errors)
        {
            string loggingErrors = null;
            if (errors.Count > 0)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("");
                foreach (var error in errors)
                {
                    stringBuilder.AppendLine("*Captured*, *Expected* : *" + (error.Item1?.ToString() ?? "null") + "*, *" + (error.Item2?.ToString() ?? "null") + "*");
                }

                loggingErrors = stringBuilder.ToString();
            }

            return loggingErrors;
        }

        /// <summary>
        /// Populates a list of expected log entries for a test variation.
        /// </summary>
        /// <param name="items">the index for the <see cref="LogEntry"/> in CompleteLogEntries of interest.</param>
        /// <returns>a <see cref="List{LogEntry}"/> that represents the expected entries for a test variation.</returns>
        public static List<LogEntry> PopulateLogEntries(int[] items)
        {
            var entries = new List<LogEntry>();
            foreach (var item in items)
            {
                entries.Add(CompleteLogEntries[item]);
            }

            return entries;
        }
    }
}
