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
    private readonly List<object[]> _rows = new();
    private readonly IReporter _reporter;

    public ConsoleTable(IReporter reporter)
    {
        _reporter = reporter;
    }

    public void AddColumns(params string[] names)
    {
        _columns.AddRange(names);
    }

    public void AddRow(params object[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        if (!_columns.Any())
        {
            throw new Exception("Columns must be set before rows can be added.");
        }

        if (_columns.Count != values.Length)
        {
            throw new Exception(
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

        var equalColumnLengths = (Console.WindowWidth / _columns.Count);

        for (var i = 0; i < maxColumnLengths.Count; i++)
        {
            if (maxColumnLengths[i] > equalColumnLengths)
            {
                maxColumnLengths[i] = equalColumnLengths;
            }
        }

        var formatRow = Enumerable.Range(0, _columns.Count)
            .Select(i => " | {" + i + ", " + maxColumnLengths[i] + "}")
            .Aggregate((previousRowColumn, nextRowColumn) => previousRowColumn + nextRowColumn) + " |";

        var columnHeaders = string.Format(CultureInfo.InvariantCulture, formatRow, _columns.ToArray());
        var rowDivider = $" {new string('-', columnHeaders.Length - 1)} ";

        // Creates a nested list of items, each representing a single jwt

        var formattedRows = new List<List<string>>();

        foreach (var jwtObject in _rows)
        {
            var stringListOfJwtItems = new List<string>();
            var jwtList = jwtObject.ToList();

            for (var i = 0; i < jwtList.Count; i++)
            {
                stringListOfJwtItems.Add(jwtList[i].ToString());
            }
            formattedRows.Add(stringListOfJwtItems);
        }

        builder.AppendLine(rowDivider);
        builder.AppendLine(columnHeaders);

        var newFormattedRows = WriteTableContent(formattedRows, maxColumnLengths, rowDivider);

        builder.AppendLine(rowDivider);

        foreach (var formattedRow in newFormattedRows)
        {
            builder.AppendLine(formattedRow);
        }

        _reporter.Output(builder.ToString());

        // Write each jwt into the table making sure that longer items are wrapped.
        static List<string> WriteTableContent(List<List<string>> rows, List<int> columnLengths, string divider)
        {
            var listOfRows = new List<string>();
            for (var i = 0; i < rows.Count; i++)
            {
                var updatedRow = rows[i];

                bool status = true;
                while (status)
                {
                    var outputRow = "";

                    for (var j = 0; j < updatedRow.Count; j++)
                    {
                        outputRow = string.Concat(outputRow, " | ");
                        var currentItem = updatedRow[j];

                        if (currentItem.Length <= columnLengths[j])
                        {
                            outputRow = string.Concat(outputRow, currentItem, new string(' ', columnLengths[j] - currentItem.Length));
                            updatedRow[j] = "";
                        }
                        else
                        {
                            outputRow = string.Concat(outputRow, currentItem.Substring(0, columnLengths[j]));
                            updatedRow[j] = currentItem.Substring(columnLengths[j]);
                        }
                    }
                    outputRow = string.Concat(outputRow, " |");
                    listOfRows.Add(outputRow);

                    // Check if all items in updated row are set to an empty string
                    if (updatedRow.All(item => item == ""))
                    {
                        status = false;
                    }
                }
                listOfRows.Add(divider);
            }
            return listOfRows;
        }
    }
}
