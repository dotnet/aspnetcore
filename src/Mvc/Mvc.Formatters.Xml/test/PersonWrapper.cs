// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
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
}
