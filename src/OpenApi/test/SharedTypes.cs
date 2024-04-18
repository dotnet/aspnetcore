// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains shared types that are used across tests, sample apps,
// and benchmark apps.

public record Todo(int Id, string Title, bool Completed, DateTime CreatedAt);

public record TodoWithDueDate(int Id, string Title, bool Completed, DateTime CreatedAt, DateTime DueDate) : Todo(Id, Title, Completed, CreatedAt);

public record Error(int code, string Message);
