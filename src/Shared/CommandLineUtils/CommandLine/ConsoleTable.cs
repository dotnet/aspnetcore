// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Extensions.CommandLineUtils;

internal sealed class ConsoleTable
{
    private readonly List<string> _columns = new();
    private readonly List<object[]> _rows = new();

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

        var formatRow = Enumerable.Range(0, _columns.Count)
            .Select(i => " | {" + i + ", " + maxColumnLengths[i] + "}")
            .Aggregate((previousRowColumn, nextRowColumn) => previousRowColumn + nextRowColumn) + " |";

        var formattedRows = _rows.Select(row => string.Format(CultureInfo.InvariantCulture, formatRow, row)).ToList();
        var columnHeaders = string.Format(CultureInfo.InvariantCulture, formatRow, _columns.ToArray());
        var rowDivider = $" {new string('-', columnHeaders.Length - 1)} ";

        builder.AppendLine(rowDivider);
        builder.AppendLine(columnHeaders);

        foreach (var formattedRow in formattedRows)
        {
            builder.AppendLine(rowDivider);
            builder.AppendLine(formattedRow);
        }

        builder.AppendLine(rowDivider);

        Console.WriteLine(builder.ToString());
    }
}
