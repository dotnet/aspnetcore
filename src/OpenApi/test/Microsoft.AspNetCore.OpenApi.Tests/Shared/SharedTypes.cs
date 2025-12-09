// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains shared types that are used across tests, sample apps,
// and benchmark apps.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Represents a to-do item.
/// </summary>
/// <param name="Id">The unique identifier of the to-do item.</param>
/// <param name="Title">The title of the to-do item.</param>
/// <param name="Completed">Indicates whether the to-do item is completed.</param>
/// <param name="CreatedAt">The date and time when the to-do item was created.</param>
internal record Todo(int Id, string Title, bool Completed, DateTime CreatedAt);

/// <summary>
/// Represents a to-do item with a due date.
/// </summary>
/// <param name="Id">The unique identifier of the to-do item.</param>
/// <param name="Title">The title of the to-do item.</param>
/// <param name="Completed">Indicates whether the to-do item is completed.</param>
/// <param name="CreatedAt">The date and time when the to-do item was created.</param>
/// <param name="DueDate">The due date of the to-do item.</param>
internal record TodoWithDueDate(int Id, string Title, bool Completed, DateTime CreatedAt, DateTime DueDate) : Todo(Id, Title, Completed, CreatedAt);

/// <summary>
/// Represents an error.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
internal record Error(int Code, string Message);

/// <summary>
/// Represents a resume upload.
/// </summary>
/// <param name="Name">The name of the resume.</param>
/// <param name="Description">The description of the resume.</param>
/// <param name="Resume">The resume file.</param>
internal record ResumeUpload(string Name, string Description, IFormFile Resume);

/// <summary>
/// Represents a result of an operation.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <param name="IsSuccessful">Indicates whether the operation was successful.</param>
/// <param name="Value">The value of the result.</param>
/// <param name="Error">The error associated with the result, if any.</param>
internal record Result<T>(bool IsSuccessful, T Value, Error Error);

/// <summary>
/// Represents a vehicle.
/// </summary>
internal class Vehicle
{
    /// <summary>
    /// Gets or sets the number of wheels.
    /// </summary>
    public int Wheels { get; set; }

    /// <summary>
    /// Gets or sets the make of the vehicle.
    /// </summary>
    public string Make { get; set; } = string.Empty;
}

/// <summary>
/// Represents a car.
/// </summary>
internal class Car : Vehicle
{
    /// <summary>
    /// Gets or sets the number of doors.
    /// </summary>
    public int Doors { get; set; }
}

/// <summary>
/// Represents a boat.
/// </summary>
internal class Boat : Vehicle
{
    /// <summary>
    /// Gets or sets the length of the boat.
    /// </summary>
    public double Length { get; set; }
}

/// <summary>
/// Represents the status of an operation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Status>))]
internal enum Status
{
    /// <summary>
    /// The operation is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// The operation is approved.
    /// </summary>
    Approved,

    /// <summary>
    /// The operation is rejected.
    /// </summary>
    Rejected
}

/// <summary>
/// Represents a proposal.
/// </summary>
internal class Proposal
{
    /// <summary>
    /// Gets or sets the proposal element.
    /// </summary>
    public required Proposal ProposalElement { get; set; }

    /// <summary>
    /// Gets or sets the stream associated with the proposal.
    /// </summary>
    public required Stream Stream { get; set; }
}

/// <summary>
/// Represents a paginated collection of items.
/// </summary>
/// <typeparam name="T">The type of items contained in the collection. Must be a reference type.</typeparam>
/// <param name="pageIndex">The current page index (zero-based).</param>
/// <param name="pageSize">The number of items per page.</param>
/// <param name="totalItems">The total number of items in the collection.</param>
/// <param name="totalPages">The total number of pages available.</param>
/// <param name="items">The collection of items for the current page.</param>
internal class PaginatedItems<T>(int pageIndex, int pageSize, long totalItems, int totalPages, IEnumerable<T> items) where T : class
{
    /// <summary>
    /// Gets or sets the current page index (zero-based).
    /// </summary>
    public int PageIndex { get; set; } = pageIndex;

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; } = pageSize;

    /// <summary>
    /// Gets or sets the total number of items in the collection.
    /// </summary>
    public long TotalItems { get; set; } = totalItems;

    /// <summary>
    /// Gets or sets the total number of pages available.
    /// </summary>
    public int TotalPages { get; set; } = totalPages;

    /// <summary>
    /// Gets or sets the collection of items for the current page.
    /// </summary>
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
    [MaxLength(10)]
    [DefaultValue(null)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Used in tests.")]
    public string? Name { get; set; }

    [Length(5, 10)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Used in tests.")]
    public string? Description { get; set; }

    [DefaultValue(true)]
    public required bool IsPrivate { get; set; }

    [MaxLength(10)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Used in tests.")]
    public IList<ProjectBoardItem>? Items { get; set; }

    [Length(5, 10)]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Used in tests.")]
    public IEnumerable<string>? Tags { get; set; }
}

internal sealed record ProjectBoardItem(string Name);

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
