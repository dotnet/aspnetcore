// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using XmlFormattersWebSite.Models;

namespace XmlFormattersWebSite;

public class PersonWrapper : IUnwrappable
{
    public PersonWrapper() { }

    public PersonWrapper(Person person)
    {
        Id = person.Id;
        Name = person.Name;
        Age = 35;
    }

    public int Id { get; set; }

    public string Name { get; set; }

    public int Age { get; set; }

    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}, {1}, {2}", Id, Name, Age);
    }

    public object Unwrap(Type declaredType)
    {
        return new Person() { Id = this.Id, Name = this.Name };
    }
}
