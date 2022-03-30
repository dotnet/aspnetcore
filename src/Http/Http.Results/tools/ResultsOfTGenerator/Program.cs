// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

var className = "Results";
var typeArgCount = 16;
var typeArgName = "TResult";
var filePath = Path.Join(Directory.GetCurrentDirectory(), $"{className}OfT.cs");

Console.WriteLine($"Will generate file at {filePath}");
Console.WriteLine("Press Enter to continue");
Console.ReadLine();

using var writer = new StreamWriter(filePath, append: false);

// File header
writer.WriteLine("// Licensed to the .NET Foundation under one or more agreements.");
writer.WriteLine("// The .NET Foundation licenses this file to you under the MIT license.");
writer.WriteLine();

// Namespace
writer.WriteLine("namespace Microsoft.AspNetCore.Http.Result;");
writer.WriteLine();

for (int i = 1; i <= typeArgCount; i++)
{
    // Class summary doc
    writer.WriteLine(@$"/// <summary>
/// Represents the result of an <see cref=""Endpoint""/> route handler delegate that can return {i.ToWords()} different <see cref=""IResult""/> types.
/// </summary>");

    // Type params docs
    for (int j = 1; j <= i; j++)
    {
        writer.WriteLine(@$"/// <typeparam name=""{typeArgName}{j}"">The {j.ToOrdinalWords()} result type.</typeparam>");
    }

    // Class declaration
    writer.Write($"public sealed class {className}<");

    // Type args
    for (int j = 1; j <= i; j++)
    {
        writer.Write($"{typeArgName}{j}");
        if (j != i)
        {
            writer.Write(", ");
        }
    }

    writer.WriteLine("> : IResult");

    // Type arg contraints
    for (int j = 1; j <= i; j++)
    {
        writer.Write($"   where {typeArgName}{j} : IResult");
        if (j != i)
        {
            writer.Write(Environment.NewLine);
        }
    }
    writer.WriteLine();
    writer.WriteLine("{");

    // Ctor
    writer.WriteLine($"    private {className}(IResult activeResult)");
    writer.WriteLine("    {");
    writer.WriteLine("        Result = activeResult;");
    writer.WriteLine("    }");
    writer.WriteLine();

    // Result property
    writer.WriteLine("    /// <summary>");
    writer.WriteLine($"    /// Gets the actual <see cref=\"IResult\"/> returned by the <see cref=\"Endpoint\"/> route handler delegate.");
    writer.WriteLine("    /// </summary>");
    writer.WriteLine("    public IResult Result { get; }");
    writer.WriteLine();

    // ExecuteAsync method
    writer.WriteLine("    /// <summary>");
    writer.WriteLine("    /// Writes an HTTP response reflecting the result.");
    writer.WriteLine("    /// </summary>");
    writer.WriteLine("    /// <param name=\"httpContext\">The <see cref=\"HttpContext\"/> for the current request.</param>");
    writer.WriteLine("    /// <returns>A <see cref=\"Task\"/> that represents the asynchronous execute operation.</returns>");
    writer.WriteLine("    public async Task ExecuteAsync(HttpContext httpContext)");
    writer.WriteLine("    {");
    writer.WriteLine("        ArgumentNullException.ThrowIfNull(httpContext, nameof(httpContext));");
    writer.WriteLine();
    writer.WriteLine("        await Result.ExecuteAsync(httpContext);");
    writer.WriteLine("    }");
    writer.WriteLine();

    // Implicit converter operators
    var sb = new StringBuilder();
    for (int j = 1; j <= i; j++)
    {
        sb.Append($"{typeArgName}{j}");
        if (j != i)
        {
            sb.Append(", ");
        }
    }
    var typeArgsList = sb.ToString();

    for (int j = 1; j <= i; j++)
    {
        writer.WriteLine("    /// <summary>");
        writer.WriteLine($"    /// Converts the <typeparamref name=\"{typeArgName}{j}\"/> to a <see cref=\"{className}{{{typeArgsList}}}\" />.");
        writer.WriteLine("    /// </summary>");
        writer.WriteLine("    /// <param name=\"result\">The result.</param>");
        writer.WriteLine($"    public static implicit operator {className}<{typeArgsList}>({typeArgName}{j} result) => new(result);");

        if (i != j)
        {
            writer.WriteLine();
        }
    }

    // Class end
    writer.WriteLine("}");

    if (i != typeArgCount)
    {
        writer.WriteLine();
    }
}

writer.Flush();

static class StringExtensions
{
    public static string ToWords(this int number) => number switch
    {
        1 => "one",
        2 => "two",
        3 => "three",
        4 => "four",
        5 => "five",
        6 => "six",
        7 => "seven",
        8 => "eight",
        9 => "nine",
        10 => "ten",
        11 => "eleven",
        12 => "twelve",
        13 => "thirteen",
        14 => "fourteen",
        15 => "fifteen",
        16 => "sixteen",
        17 => "seventeen",
        18 => "eighteen",
        19 => "nineteen",
        20 => "twenty",
        _ => "!!unsupported!!"
    };

    public static string ToOrdinalWords(this int number) => number switch
    {
        1 => "first",
        2 => "second",
        3 => "third",
        4 => "fourth",
        5 => "fifth",
        6 => "sixth",
        7 => "seventh",
        8 => "eighth",
        9 => "ninth",
        10 => "tenth",
        11 => "eleventh",
        12 => "twelfth",
        13 => "thirteenth",
        14 => "fourteenth",
        15 => "fifteenth",
        16 => "sixteenth",
        17 => "seventeenth",
        18 => "eighteenth",
        19 => "nineteenth",
        20 => "twentieth",
        _ => "!!unsupported!!"
    };
}

/* TEMPLATE
/// <summary>
/// Represents the result of an <see cref="Endpoint"/> route handler delegate that can return ten different <see cref="IResult"/> types.
/// </summary>
/// <typeparam name="TResult1">The first result type.</typeparam>
/// <typeparam name="TResult2">The second result type.</typeparam>
/// <typeparam name="TResult3">The third result type.</typeparam>
/// <typeparam name="TResult4">The fourth result type.</typeparam>
/// <typeparam name="TResult5">The fifth result type.</typeparam>
/// <typeparam name="TResult6">The sixth result type.</typeparam>
/// <typeparam name="TResult7">The seventh result type.</typeparam>
/// <typeparam name="TResult8">The eighth result type.</typeparam>
/// <typeparam name="TResult9">The ninth result type.</typeparam>
/// <typeparam name="TResult10">The tenth result type.</typeparam>
public sealed class Results<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10> : IResult
    where TResult1 : IResult
    where TResult2 : IResult
    where TResult3 : IResult
    where TResult4 : IResult
    where TResult5 : IResult
    where TResult6 : IResult
    where TResult7 : IResult
    where TResult8 : IResult
    where TResult9 : IResult
    where TResult10 : IResult
{
    private Results(IResult activeResult)
    {
        Result = activeResult;
    }

    /// <summary>
    /// Gets the actual <see cref="IResult"/> returned by the <see cref="Endpoint"/> route handler delegate.
    /// </summary>
    public IResult Result { get; }

    /// <summary>
    /// Writes an HTTP response reflecting the result.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous execute operation.</returns>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext, nameof(httpContext));

        await Result.ExecuteAsync(httpContext);
    }

    /// <summary>
    /// Converts the <typeparamref name="TResult1"/> to a <see cref="Results{TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10}"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    public static implicit operator Results<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10>(TResult1 result) => new(result);

    /// <summary>
    /// Converts the <typeparamref name="TResult2"/> to a <see cref="Results{TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10}"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    public static implicit operator Results<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10>(TResult2 result) => new(result);

    /// <summary>
    /// Converts the <typeparamref name="TResult3"/> to a <see cref="Results{TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10}"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    public static implicit operator Results<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10>(TResult3 result) => new(result);

    /// <summary>
    /// Converts the <typeparamref name="TResult4"/> to a <see cref="Results{TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10}"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    public static implicit operator Results<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10>(TResult4 result) => new(result);

    /// <summary>
    /// Converts the <typeparamref name="TResult5"/> to a <see cref="Results{TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10}"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    public static implicit operator Results<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10>(TResult5 result) => new(result);

    /// <summary>
    /// Converts the <typeparamref name="TResult6"/> to a <see cref="Results{TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10}"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    public static implicit operator Results<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10>(TResult6 result) => new(result);

    /// <summary>
    /// Converts the <typeparamref name="TResult7"/> to a <see cref="Results{TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10}"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    public static implicit operator Results<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10>(TResult7 result) => new(result);

    /// <summary>
    /// Converts the <typeparamref name="TResult8"/> to a <see cref="Results{TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10}"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    public static implicit operator Results<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10>(TResult8 result) => new(result);

    /// <summary>
    /// Converts the <typeparamref name="TResult9"/> to a <see cref="Results{TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10}"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    public static implicit operator Results<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10>(TResult9 result) => new(result);

    /// <summary>
    /// Converts the <typeparamref name="TResult10"/> to a <see cref="Results{TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10}"/>.
    /// </summary>
    /// <param name="result">The result.</param>
    public static implicit operator Results<TResult1, TResult2, TResult3, TResult4, TResult5, TResult6, TResult7, TResult8, TResult9, TResult10>(TResult10 result) => new(result);
 */
