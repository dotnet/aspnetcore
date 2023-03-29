// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

public class BindingContext
{
    private Dictionary<string, IEnumerable<string>>? _bindingErrors;

    public BindingContext(string name)
    {
        Name = name;
    }

    public IReadOnlyDictionary<string, IEnumerable<string>> BindingErrors
        => _bindingErrors ??= new();

    internal void AddBindingError(string name, string message)
    {
        if(!BindingErrors.TryGetValue(name, out var value))
        {
            var newList = new List<string>();
            _bindingErrors ??= new();
            _bindingErrors.Add(name, newList);
            newList.Add(message);
        }
        else
        {
            var list = (IList<string>)value;
            list.Add(message);
        }
    }

    public string Name { get; }
}
