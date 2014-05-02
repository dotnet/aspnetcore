// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Microsoft.AspNet.FeatureModel
{
    public class FeatureObject : IFeatureCollection
    {
        private readonly object _instance;

        public FeatureObject(object instance)
        {
            _instance = instance;
        }

        public void Dispose()
        {
            var disposable = _instance as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        public object GetInterface(Type type)
        {
            if (type.IsInstanceOfType(_instance))
            {
                return _instance;
            }

            return null;
        }

        public void SetInterface(Type type, object feature)
        {
            throw new NotImplementedException();
        }

        public int Revision
        {
            get { return 0; }
        }

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        {
            return GetTypeInterfaces()
                .Select(interfaceType => new KeyValuePair<Type, object>(interfaceType, _instance))
                .GetEnumerator();
        }

        private IEnumerable<Type> GetTypeInterfaces()
        {
            return _instance.GetType()
                .GetTypeInfo()
                .ImplementedInterfaces;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<Type, object> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<Type, object> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<Type, object>[] array, int arrayIndex)
        {
            var enumerator = GetTypeInterfaces().GetEnumerator();
            for (var index = 0; index != arrayIndex; ++index)
            {
                if (!enumerator.MoveNext())
                {
                    throw new IndexOutOfRangeException();
                }
            }

            for (var index = 0; index != array.Length; ++index)
            {
                if (!enumerator.MoveNext())
                {
                    throw new IndexOutOfRangeException();
                }
                array[index] = new KeyValuePair<Type, object>(enumerator.Current, _instance);
            }
        }

        public bool Remove(KeyValuePair<Type, object> item)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return GetTypeInterfaces().Count(); }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool ContainsKey(Type key)
        {
            return key.GetTypeInfo().IsAssignableFrom(_instance.GetType().GetTypeInfo());
        }

        public void Add(Type key, object value)
        {
            throw new NotImplementedException();
        }

        public bool Remove(Type key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(Type key, out object value)
        {
            value = GetInterface(key);
            return value != null;
        }

        public object this[Type key]
        {
            get { return GetInterface(key); }
            set { throw new NotImplementedException(); }
        }

        public ICollection<Type> Keys
        {
            get { return GetTypeInterfaces().ToArray(); }
        }

        public ICollection<object> Values
        {
            get
            {
                var length = GetTypeInterfaces().Count();
                var array = new object[length];
                for (var index = 0; index != length; ++index)
                {
                    array[index] = _instance;
                }
                return array;
            }
        }
    }
}