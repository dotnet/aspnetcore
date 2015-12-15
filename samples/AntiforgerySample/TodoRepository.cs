// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace AntiforgerySample
{
    public class TodoRepository
    {
        private List<TodoItem> _items;

        public TodoRepository()
        {
            _items = new List<TodoItem>()
            {
                new TodoItem() { Name = "Mow the lawn" },
                new TodoItem() { Name = "Do the dishes" },
            };
        }

        public IEnumerable<TodoItem> GetItems()
        {
            return _items;
        }

        public void Add(TodoItem item)
        {
            _items.Add(item);
        }
    }
}
