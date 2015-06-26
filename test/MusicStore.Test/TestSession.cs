using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;

namespace MusicStore.Controllers
{
    internal class TestSession : ISession
    {
        private Dictionary<string, byte[]> _store
            = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<string> Keys { get { return _store.Keys; } }

        public void Clear()
        {
            _store.Clear();
        }

        public Task CommitAsync()
        {
            return Task.FromResult(0);
        }

        public Task LoadAsync()
        {
            return Task.FromResult(0);
        }

        public void Remove(string key)
        {
            _store.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _store[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _store.TryGetValue(key, out value);
        }
    }
}
