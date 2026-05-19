// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

public class PersonWrapperProvider : IWrapperProvider
{
    public object Wrap(object obj)
    {
        var person = obj as Person;

        if (person == null)
        {
            return obj;
        }

        return new PersonWrapper(person);
    }

    public Type WrappingType
    {
        get
        {
            return typeof(PersonWrapper);
        }
    }
}
