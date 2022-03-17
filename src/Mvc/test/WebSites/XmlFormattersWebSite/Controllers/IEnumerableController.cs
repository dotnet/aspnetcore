// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using XmlFormattersWebSite.Models;

namespace XmlFormattersWebSite.Controllers;

public class IEnumerableController : Controller
{
    public IEnumerable<int> ValueTypes()
    {
        return new[] { 10, 20 };
    }

    public IEnumerable<string> NonWrappedTypes()
    {
        return new[] { "value1", "value2" };
    }

    public IEnumerable<string> NonWrappedTypes_Empty()
    {
        return new string[] { };
    }

    public IEnumerable<Person> WrappedTypes()
    {
        return new[] {
                new Person() { Id = 10, Name = "Mike" },
                new Person() { Id = 11, Name = "Jimmy" }
            };
    }

    public IEnumerable<Person> WrappedTypes_Empty()
    {
        return new Person[] { };
    }

    public IEnumerable<string> NonWrappedTypes_NullInstance()
    {
        return null;
    }

    public IEnumerable<Person> WrappedTypes_NullInstance()
    {
        return null;
    }

    public IEnumerable<SerializableError> SerializableErrors()
    {
        List<SerializableError> errors = new List<SerializableError>();
        var error1 = new SerializableError();
        error1.Add("key1", "key1-error");
        error1.Add("key2", "key2-error");

        var error2 = new SerializableError();
        error2.Add("key3", "key1-error");
        error2.Add("key4", "key2-error");
        errors.Add(error1);
        errors.Add(error2);
        return errors;
    }
}
