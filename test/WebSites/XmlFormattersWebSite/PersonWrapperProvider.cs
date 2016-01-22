// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using XmlFormattersWebSite.Models;

namespace XmlFormattersWebSite
{
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
}