// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Microsoft.AspNet.FeatureModel
{
    public class FeatureCollection : IFeatureCollection
    {
        private readonly IFeatureCollection _defaults;
        private readonly IDictionary<Type, object> _featureByFeatureType = new Dictionary<Type, object>();
        private readonly IDictionary<string, Type> _featureTypeByName = new Dictionary<string, Type>();
        private readonly object _containerSync = new Object();
        private int _containerRevision;

        public FeatureCollection()
        {
        }

        public FeatureCollection(IFeatureCollection defaults)
        {
            _defaults = defaults;
        }

        public object GetInterface()
        {
            return GetInterface(null);
        }

        public object GetInterface(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            object feature;
            if (_featureByFeatureType.TryGetValue(type, out feature))
            {
                return feature;
            }

            Type actualType;
            if (_featureTypeByName.TryGetValue(type.FullName, out actualType))
            {
                if (_featureByFeatureType.TryGetValue(actualType, out feature))
                {
                    var isInstanceOfType = type.IsInstanceOfType(feature);

                    if (isInstanceOfType)
                    {
                        return feature;
                    }

                    return null;
                }
            }

            if (_defaults != null && _defaults.TryGetValue(type, out feature))
            {
                return feature;
            }
            return null;
        }

        void SetInterface(Type type, object feature)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (feature == null) throw new ArgumentNullException("feature");

            lock (_containerSync)
            {
                Type priorFeatureType;
                if (_featureTypeByName.TryGetValue(type.FullName, out priorFeatureType))
                {
                    if (priorFeatureType == type)
                    {
                        _featureByFeatureType[type] = feature;
                    }
                    else
                    {
                        _featureTypeByName[type.FullName] = type;
                        _featureByFeatureType.Remove(priorFeatureType);
                        _featureByFeatureType.Add(type, feature);
                    }
                }
                else
                {
                    _featureTypeByName.Add(type.FullName, type);
                    _featureByFeatureType.Add(type, feature);
                }
                Interlocked.Increment(ref _containerRevision);
            }
        }

        public virtual int Revision
        {
            get { return _containerRevision; }
        }

        public void Dispose()
        {
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
            SetInterface(item.Key, item.Value);
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<Type, object> item)
        {
            object value;
            return TryGetValue(item.Key, out value) && Equals(item.Value, value);
        }

        public void CopyTo(KeyValuePair<Type, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<Type, object> item)
        {
            return Contains(item) && Remove(item.Key);
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool ContainsKey(Type key)
        {
            if (key == null) throw new ArgumentNullException("key");
            return GetInterface(key) != null;
        }

        public void Add(Type key, object value)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");
            if (ContainsKey(key))
            {
                throw new ArgumentException();
            }
            SetInterface(key, value);
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
            set { SetInterface(key, value); }
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