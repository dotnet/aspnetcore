
using System;
using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.Sockets
{
    public class ConnectionMetadata
    {
        private ConcurrentDictionary<string, object> _metadata = new ConcurrentDictionary<string, object>();

        public Format Format { get; set; } = Format.Text;

        public object this[string key]
        {
            get
            {
                object value;
                _metadata.TryGetValue(key, out value);
                return value;
            }
            set
            {
                _metadata[key] = value;
            }
        }

        public T GetOrAdd<T>(string key, Func<string, T> factory)
        {
            return (T)_metadata.GetOrAdd(key, k => factory(k));
        }

        public T Get<T>(string key)
        {
            return (T)this[key];
        }
    }
}
