// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains shared types that are used across tests, sample apps,
// and benchmark apps.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

internal record Todo(int Id, string Title, bool Completed, DateTime CreatedAt);

internal record TodoWithDueDate(int Id, string Title, bool Completed, DateTime CreatedAt, DateTime DueDate) : Todo(Id, Title, Completed, CreatedAt);

internal record Error(int Code, string Message);

internal record ResumeUpload(string Name, string Description, IFormFile Resume);

internal record Result<T>(bool IsSuccessful, T Value, Error Error);

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

internal class RequiredTodo
{
    [Required]
    public string Title { get; set; } = string.Empty;
    [Required]
    public bool Completed { get; set; }
    public string Assignee { get; set; } = string.Empty;
}

#nullable enable
internal class ProjectBoard
{
    [Range(1, 100)]
    [DefaultValue(null)]
    public int Id { get; set; }

    [MinLength(5)]
    [DefaultValue(null)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Used in tests.")]
    public string? Name { get; set; }

    [DefaultValue(true)]
    public required bool IsPrivate { get; set; }
}
#nullable restore

internal class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

internal class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
