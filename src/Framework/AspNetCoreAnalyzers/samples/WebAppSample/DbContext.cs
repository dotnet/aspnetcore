// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public class DbContext
{
    public IList<Todo> Todos { get; } = new List<Todo>();
    public IList<Book> Books { get; } = new List<Book>();
}

public static class ListExtensions
{
    public static T Find<T>(this IList<T> list, int id)
    {
        return default!;
    }
}

public class Todo
{
    public string Text { get; set; }
}

public class Book
{
    public string Text { get; set; }
}
