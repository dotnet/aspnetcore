// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TestTypeInfo : ITypeInfo
    {
        public string FullName { get; set; }

        public bool IsAbstract
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsGenericType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsPublic
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool ImplementsInterface(ITypeInfo other)
        {
            throw new NotImplementedException();
        }

        public string Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<IPropertyInfo> Properties
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool Equals(ITypeInfo other)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TAttribute> GetCustomAttributes<TAttribute>() where TAttribute : Attribute
        {
            throw new NotImplementedException();
        }

        public ITypeInfo[] GetGenericDictionaryParameters()
        {
            throw new NotImplementedException();
        }
    }
}
