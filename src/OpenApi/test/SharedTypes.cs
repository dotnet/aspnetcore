// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains shared types that are used across tests, sample apps,
// and benchmark apps.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

internal record Todo(int Id, string Title, bool Completed, DateTime CreatedAt);

internal record TodoWithDueDate(int Id, string Title, bool Completed, DateTime CreatedAt, DateTime DueDate) : Todo(Id, Title, Completed, CreatedAt);

internal record Error(int Code, string Message);

internal record ResumeUpload(string Name, string Description, IFormFile Resume);

internal record Result<T>(bool IsSuccessful, T Value, Error Error);

[JsonDerivedType(typeof(Triangle), typeDiscriminator: "triangle")]
[JsonDerivedType(typeof(Square), typeDiscriminator: "square")]
internal class Shape
{
    internal string Color { get; set; } = string.Empty;
    internal int Sides { get; set; }
}

internal class Triangle : Shape
{
    internal double Hypotenuse { get; set; }
}
internal class Square : Shape
{
    internal double Area { get; set; }
}

internal class Vehicle
{
    public int Wheels { get; set; }
    public string Make { get; set; } = string.Empty;
}

internal class Car : Vehicle
{
    public int Doors { get; set; }
}

internal class Boat : Vehicle
{
    public double Length { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter<Status>))]
internal enum Status
{
    Pending,
    Approved,
    Rejected
}

internal class Proposal
{
    public required Proposal ProposalElement { get; set; }
    public required Stream Stream { get; set; }
}

internal class PaginatedItems<T>(int pageIndex, int pageSize, long totalItems, int totalPages, IEnumerable<T> items) where T : class
{
    public int PageIndex { get; set; } = pageIndex;
    public int PageSize { get; set; } = pageSize;
    public long TotalItems { get; set; } = totalItems;
    public int TotalPages { get; set; } = totalPages;
    public IEnumerable<T> Items { get; set; } = items;
}
