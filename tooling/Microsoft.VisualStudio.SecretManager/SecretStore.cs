// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.SecretManager
{
    /// <summary>
    /// Provides read and write access to the secrets.json file for local user secrets.
    /// This is not thread-safe.
    /// This object is meant to have a short lifetime.
    /// When calling <see cref="SaveAsync(CancellationToken)"/>, this will overwrite the secrets.json file. It does not check for concurrency issues if another process has edited this file.
    /// </summary>
    internal class SecretStore : IDisposable
    {
        private Dictionary<string, string> _secrets;
        private string _fileDir;
        private string _filePath;
        private bool _isDirty;
        private volatile bool _disposed;

        public SecretStore(string userSecretsId)
        {
            _filePath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);
            _fileDir = Path.GetDirectoryName(_filePath);
        }

        public IReadOnlyCollection<string> ReadOnlyKeys
        {
            get
            {
                EnsureNotDisposed();
                return _secrets.Keys;
            }
        }

        public IReadOnlyDictionary<string, string> Values
        {
            get
            {
                EnsureNotDisposed();

                return _secrets;
            }
        }

        public bool ContainsKey(string key)
        {
            EnsureNotDisposed();

            return _secrets.ContainsKey(key);
        }

        public string Get(string name)
        {
            EnsureNotDisposed();

            return _secrets[name];
        }

        public void Set(string key, string value)
        {
            EnsureNotDisposed();

            _isDirty = true;
            _secrets[key] = value;
        }

        public bool Remove(string key)
        {
            EnsureNotDisposed();
            _isDirty = true;
            return _secrets.Remove(key);
        }

        public async Task LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await TaskScheduler.Default;

            EnsureNotDisposed();

            string text = null;

            if (File.Exists(_filePath))
            {
                text = File.ReadAllText(_filePath);
            }

            _secrets = DeserializeJson(text);
        }

        public async Task SaveAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await TaskScheduler.Default;

            EnsureNotDisposed();

            if (!_isDirty)
            {
                return;
            }

            Directory.CreateDirectory(_fileDir);
            File.WriteAllText(_filePath, Stringify(_secrets), Encoding.UTF8);

            _isDirty = false;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SecretStore));
            }
        }

        private static string Stringify(Dictionary<string, string> secrets)
        {
            var contents = new JObject();
            if (secrets != null)
            {
                foreach (var secret in secrets)
                {
                    contents[secret.Key] = secret.Value;
                }
            }

            return contents.ToString();
        }

        private static Dictionary<string, string> DeserializeJson(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            using (var stream = new MemoryStream())
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                stream.Write(bytes, 0, bytes.Length);
                stream.Position = 0;

                // might throw FormatException if JSON is malformed.
                var data = JsonConfigurationFileParser.Parse(stream);

                return new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase);
            }
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
