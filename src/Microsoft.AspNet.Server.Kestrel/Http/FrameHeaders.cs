using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public abstract class FrameHeaders : IDictionary<string, StringValues>
    {
        protected Dictionary<string, StringValues> MaybeUnknown;

        protected Dictionary<string, StringValues> Unknown => MaybeUnknown ?? (MaybeUnknown = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase));

        protected static StringValues AppendValue(StringValues existing, string append)
        {
            return StringValues.Concat(existing, append);
        }

        protected virtual int GetCountFast()
        { throw new NotImplementedException(); }

        protected virtual StringValues GetValueFast(string key)
        { throw new NotImplementedException(); }

        protected virtual bool TryGetValueFast(string key, out StringValues value)
        { throw new NotImplementedException(); }

        protected virtual void SetValueFast(string key, StringValues value)
        { throw new NotImplementedException(); }

        protected virtual void AddValueFast(string key, StringValues value)
        { throw new NotImplementedException(); }

        protected virtual bool RemoveFast(string key)
        { throw new NotImplementedException(); }

        protected virtual void ClearFast()
        { throw new NotImplementedException(); }

        protected virtual void CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        { throw new NotImplementedException(); }

        protected virtual IEnumerator<KeyValuePair<string, StringValues>> GetEnumeratorFast()
        { throw new NotImplementedException(); }


        StringValues IDictionary<string, StringValues>.this[string key]
        {
            get
            {
                return GetValueFast(key);
            }

            set
            {
                SetValueFast(key, value);
            }
        }

        int ICollection<KeyValuePair<string, StringValues>>.Count => GetCountFast();

        bool ICollection<KeyValuePair<string, StringValues>>.IsReadOnly => false;

        ICollection<string> IDictionary<string, StringValues>.Keys => ((IDictionary<string, StringValues>)this).Select(x => x.Key).ToList();

        ICollection<StringValues> IDictionary<string, StringValues>.Values => ((IDictionary<string, StringValues>)this).Select(x => x.Value).ToList();

        void ICollection<KeyValuePair<string, StringValues>>.Add(KeyValuePair<string, StringValues> item)
        {
            AddValueFast(item.Key, item.Value);
        }

        void IDictionary<string, StringValues>.Add(string key, StringValues value)
        {
            AddValueFast(key, value);
        }

        void ICollection<KeyValuePair<string, StringValues>>.Clear()
        {
            ClearFast();
        }

        bool ICollection<KeyValuePair<string, StringValues>>.Contains(KeyValuePair<string, StringValues> item)
        {
            StringValues value;
            return
                TryGetValueFast(item.Key, out value) &&
                object.Equals(value, item.Value);
        }

        bool IDictionary<string, StringValues>.ContainsKey(string key)
        {
            StringValues value;
            return TryGetValueFast(key, out value);
        }

        void ICollection<KeyValuePair<string, StringValues>>.CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            CopyToFast(array, arrayIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorFast();
        }

        IEnumerator<KeyValuePair<string, StringValues>> IEnumerable<KeyValuePair<string, StringValues>>.GetEnumerator()
        {
            return GetEnumeratorFast();
        }

        bool ICollection<KeyValuePair<string, StringValues>>.Remove(KeyValuePair<string, StringValues> item)
        {
            StringValues value;
            return
                TryGetValueFast(item.Key, out value) &&
                object.Equals(value, item.Value) &&
                RemoveFast(item.Key);
        }

        bool IDictionary<string, StringValues>.Remove(string key)
        {
            return RemoveFast(key);
        }

        bool IDictionary<string, StringValues>.TryGetValue(string key, out StringValues value)
        {
            return TryGetValueFast(key, out value);
        }
    }

    public partial class FrameRequestHeaders : FrameHeaders
    {
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        protected override IEnumerator<KeyValuePair<string, StringValues>> GetEnumeratorFast()
        {
            return GetEnumerator();
        }

        public partial struct Enumerator : IEnumerator<KeyValuePair<string, StringValues>>
        {
            FrameRequestHeaders _collection;
            long _bits;
            int _state;
            KeyValuePair<string, StringValues> _current;
            bool _hasUnknown;
            Dictionary<string, StringValues>.Enumerator _unknownEnumerator;

            internal Enumerator(FrameRequestHeaders collection)
            {
                _collection = collection;
                _bits = collection._bits;
                _state = 0;
                _current = default(KeyValuePair<string, StringValues>);
                _hasUnknown = collection.MaybeUnknown != null;
                _unknownEnumerator = _hasUnknown
                    ? collection.MaybeUnknown.GetEnumerator()
                    : default(Dictionary<string, StringValues>.Enumerator);
            }

            public KeyValuePair<string, StringValues> Current => _current;

            object IEnumerator.Current => _current;

            public void Dispose()
            {
            }

            public void Reset()
            {
                _state = 0;
            }
        }
    }

    public partial class FrameResponseHeaders : FrameHeaders
    {
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        protected override IEnumerator<KeyValuePair<string, StringValues>> GetEnumeratorFast()
        {
            return GetEnumerator();
        }

        public partial struct Enumerator : IEnumerator<KeyValuePair<string, StringValues>>
        {
            FrameResponseHeaders _collection;
            long _bits;
            int _state;
            KeyValuePair<string, StringValues> _current;
            bool _hasUnknown;
            Dictionary<string, StringValues>.Enumerator _unknownEnumerator;

            internal Enumerator(FrameResponseHeaders collection)
            {
                _collection = collection;
                _bits = collection._bits;
                _state = 0;
                _current = default(KeyValuePair<string, StringValues>);
                _hasUnknown = collection.MaybeUnknown != null;
                _unknownEnumerator = _hasUnknown
                    ? collection.MaybeUnknown.GetEnumerator()
                    : default(Dictionary<string, StringValues>.Enumerator);
            }

            public KeyValuePair<string, StringValues> Current => _current;

            object IEnumerator.Current => _current;

            public void Dispose()
            {
            }

            public void Reset()
            {
                _state = 0;
            }
        }
    }
}
