using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public partial class FrameRequestHeaders : FrameHeaders
    {
    }

    public partial class FrameResponseHeaders : FrameHeaders
    {
    }

    public abstract class FrameHeaders : IDictionary<string, string[]>
    {
        protected Dictionary<string, string[]> Unknown = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

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

        protected virtual IEnumerable<KeyValuePair<string, string[]>> EnumerateFast()
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

        ICollection<string> IDictionary<string, string[]>.Keys => EnumerateFast().Select(x => x.Key).ToList();

        ICollection<string[]> IDictionary<string, string[]>.Values => EnumerateFast().Select(x => x.Value).ToList();

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
            return EnumerateFast().GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, string[]>> IEnumerable<KeyValuePair<string, string[]>>.GetEnumerator()
        {
            return EnumerateFast().GetEnumerator();
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
}
