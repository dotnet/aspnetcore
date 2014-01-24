using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNet.FeatureModel.Implementation;

namespace Microsoft.AspNet.FeatureModel
{
    public class InterfaceObject : IInterfaceDictionary
    {
        private readonly object _instance;

        public InterfaceObject(object instance)
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
#if NET45
            if (type.IsInstanceOfType(_instance))
            {
                return _instance;
            }
            
            foreach (var interfaceType in _instance.GetType().GetInterfaces())
            {
                if (interfaceType.FullName == type.FullName)
                {
                    return Converter.Convert(interfaceType, type, _instance);
                }
            }
#endif
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<Type, object> item)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool ContainsKey(Type key)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public object this[Type key]
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public ICollection<Type> Keys
        {
            get { throw new NotImplementedException(); }
        }

        public ICollection<object> Values
        {
            get { throw new NotImplementedException(); }
        }
    }
}