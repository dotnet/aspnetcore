using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public abstract class FrameHeaders : IDictionary<string, string[]>
    {
        protected Dictionary<string, string[]> MaybeUnknown;

        protected Dictionary<string, string[]> Unknown => MaybeUnknown ?? (MaybeUnknown = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase));

        protected static string[] AppendValue(string[] existing, string append)
        {
            var appended = new string[existing.Length + 1];
            Array.Copy(existing, appended, existing.Length);
            appended[existing.Length] = append;
            return appended;
        }

        protected virtual int GetCountFast()
        { throw new NotImplementedException(); }

        protected virtual string[] GetValueFast(string key)
        { throw new NotImplementedException(); }

        protected virtual bool TryGetValueFast(string key, out string[] value)
        { throw new NotImplementedException(); }

        protected virtual void SetValueFast(string key, string[] value)
        { throw new NotImplementedException(); }

        protected virtual void AddValueFast(string key, string[] value)
        { throw new NotImplementedException(); }

        protected virtual bool RemoveFast(string key)
        { throw new NotImplementedException(); }

        protected virtual void ClearFast()
        { throw new NotImplementedException(); }

        protected virtual void CopyToFast(KeyValuePair<string, string[]>[] array, int arrayIndex)
        { throw new NotImplementedException(); }

        protected virtual IEnumerator<KeyValuePair<string, string[]>> GetEnumeratorFast()
        { throw new NotImplementedException(); }


        string[] IDictionary<string, string[]>.this[string key]
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

        int ICollection<KeyValuePair<string, string[]>>.Count => GetCountFast();

        bool ICollection<KeyValuePair<string, string[]>>.IsReadOnly => false;

        ICollection<string> IDictionary<string, string[]>.Keys => ((IDictionary<string,string[]>)this).Select(x => x.Key).ToList();

        ICollection<string[]> IDictionary<string, string[]>.Values => ((IDictionary<string, string[]>)this).Select(x => x.Value).ToList();

        void ICollection<KeyValuePair<string, string[]>>.Add(KeyValuePair<string, string[]> item)
        {
            AddValueFast(item.Key, item.Value);
        }

        void IDictionary<string, string[]>.Add(string key, string[] value)
        {
            AddValueFast(key, value);
        }

        void ICollection<KeyValuePair<string, string[]>>.Clear()
        {
            ClearFast();
        }

        bool ICollection<KeyValuePair<string, string[]>>.Contains(KeyValuePair<string, string[]> item)
        {
            string[] value;
            return
                TryGetValueFast(item.Key, out value) &&
                object.Equals(value, item.Value);
        }

        bool IDictionary<string, string[]>.ContainsKey(string key)
        {
            string[] value;
            return TryGetValueFast(key, out value);
        }

        void ICollection<KeyValuePair<string, string[]>>.CopyTo(KeyValuePair<string, string[]>[] array, int arrayIndex)
        {
            CopyToFast(array, arrayIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorFast();
        }

        IEnumerator<KeyValuePair<string, string[]>> IEnumerable<KeyValuePair<string, string[]>>.GetEnumerator()
        {
            return GetEnumeratorFast();
        }

        bool ICollection<KeyValuePair<string, string[]>>.Remove(KeyValuePair<string, string[]> item)
        {
            string[] value;
            return
                TryGetValueFast(item.Key, out value) &&
                object.Equals(value, item.Value) &&
                RemoveFast(item.Key);
        }

        bool IDictionary<string, string[]>.Remove(string key)
        {
            return RemoveFast(key);
        }

        bool IDictionary<string, string[]>.TryGetValue(string key, out string[] value)
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

        protected override IEnumerator<KeyValuePair<string, string[]>> GetEnumeratorFast()
        {
            return GetEnumerator();
        }

        public partial struct Enumerator : IEnumerator<KeyValuePair<string, string[]>>
        {
            FrameRequestHeaders _collection;
            long _bits;
            int _state;
            KeyValuePair<string, string[]> _current;
            bool _hasUnknown;
            Dictionary<string, string[]>.Enumerator _unknownEnumerator;

            internal Enumerator(FrameRequestHeaders collection)
            {
                _collection = collection;
                _bits = collection._bits;
                _state = 0;
                _current = default(KeyValuePair<string, string[]>);
                _hasUnknown = collection.MaybeUnknown != null;
                _unknownEnumerator = _hasUnknown
                    ? collection.MaybeUnknown.GetEnumerator()
                    : default(Dictionary<string, string[]>.Enumerator);
            }

            public KeyValuePair<string, string[]> Current => _current;

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

        protected override IEnumerator<KeyValuePair<string, string[]>> GetEnumeratorFast()
        {
            return GetEnumerator();
        }

        public partial struct Enumerator : IEnumerator<KeyValuePair<string, string[]>>
        {
            FrameResponseHeaders _collection;
            long _bits;
            int _state;
            KeyValuePair<string, string[]> _current;
            bool _hasUnknown;
            Dictionary<string, string[]>.Enumerator _unknownEnumerator;

            internal Enumerator(FrameResponseHeaders collection)
            {
                _collection = collection;
                _bits = collection._bits;
                _state = 0;
                _current = default(KeyValuePair<string, string[]>);
                _hasUnknown = collection.MaybeUnknown != null;
                _unknownEnumerator = _hasUnknown
                    ? collection.MaybeUnknown.GetEnumerator()
                    : default(Dictionary<string, string[]>.Enumerator);
            }

            public KeyValuePair<string, string[]> Current => _current;

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
