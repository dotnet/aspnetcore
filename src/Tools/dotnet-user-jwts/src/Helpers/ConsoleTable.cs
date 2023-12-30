// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.CommandLineUtils;

internal sealed class ConsoleTable
{
    private readonly List<string> _columns = new();
    private readonly List<string[]> _rows = new();
    private readonly IReporter _reporter;

    public ConsoleTable(IReporter reporter)
    {
        _reporter = reporter;
    }

    public void AddColumns(params string[] names)
    {
        _columns.AddRange(names);
    }

    public void AddRow(params string[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (!_columns.Any())
        {
            throw new InvalidOperationException("Columns must be set before rows can be added.");
        }

        if (_columns.Count != values.Length)
        {
            throw new InvalidOperationException(
                $"The number of columns in the table '{_columns.Count}' does not match the number of columns in the row '{values.Length}'.");
        }

        _rows.Add(values);
    }

    public void Write()
    {
        var builder = new StringBuilder();

        var maxColumnLengths = _columns
            .Select((t, i) => _rows.Select(x => x[i])
                .Concat(new[] { _columns[i] })
                .Where(x => x != null)
                .Select(x => x!.ToString()!.Length).Max())
            .ToList();

        // The table borders constructed using "|" have whitespaces before and after.
        // This number accounts for those spaces to ensure that the table width is not longer than the console window's width.
        var EXCESS_LENGTH_CREATED_BY_BORDERS = 4;

        var equalColumnLengths = Math.Max((Console.WindowWidth / _columns.Count) - EXCESS_LENGTH_CREATED_BY_BORDERS, 5);

        var excessLength = 0;
        var numberOfColumnsThatNeedMoreLength = 0;

        // Keep track of the excess length left behind by narrow columns and the number of columns that could use the extra length.
        for (var i = 0; i < maxColumnLengths.Count; i++)
        {
            if (maxColumnLengths[i] < equalColumnLengths)
            {
                excessLength += equalColumnLengths - maxColumnLengths[i];
            }
            else
            {
                numberOfColumnsThatNeedMoreLength += 1;
                maxColumnLengths[i] = equalColumnLengths;
            }
        }

        // Share the excess length amongst the columns that could use it.
        for (var i = 0; i < maxColumnLengths.Count; i++)
        {
            if (maxColumnLengths[i] == equalColumnLengths)
            {
                maxColumnLengths[i] += excessLength / numberOfColumnsThatNeedMoreLength;
            }
        }

        var formatRow = Enumerable.Range(0, _columns.Count)
            .Select(i => " | {" + i + ", " + maxColumnLengths[i] + "}")
            .Aggregate((previousRowColumn, nextRowColumn) => previousRowColumn + nextRowColumn) + " |";

        var columnHeaders = string.Format(CultureInfo.InvariantCulture, formatRow, _columns.ToArray());
        var rowDivider = $" {new string('-', columnHeaders.Length - 1)} ";

        builder.AppendLine(rowDivider);
        builder.AppendLine(columnHeaders);

        WriteTableContent(_rows, maxColumnLengths, rowDivider, builder);

        void WriteTableContent(List<string[]> rows, List<int> columnLengths, string divider, StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine(divider);

            for (var i = 0; i < rows.Count; i++)
            {
                while (!rows[i].All(i => i == string.Empty))
                {
                    var outputRow = string.Empty;

                    for (var j = 0; j < rows[i].Length; j++)
                    {
                        outputRow = string.Concat(outputRow, " | ");

                        if (rows[i][j].Length <= columnLengths[j])
                        {
                            outputRow = string.Concat(outputRow, rows[i][j], new string(' ', columnLengths[j] - rows[i][j].Length));
                            rows[i][j] = string.Empty;
                        }
                        else
                        {
                            outputRow = string.Concat(outputRow, rows[i][j].Substring(0, columnLengths[j]));
                            rows[i][j] = rows[i][j].Substring(columnLengths[j]);
                        }
                    }
                    outputRow = string.Concat(outputRow, " |");
                    stringBuilder.AppendLine(outputRow);
                }
                stringBuilder.AppendLine(divider);
            }
            _reporter.Output(builder.ToString());
        }
    }
}
