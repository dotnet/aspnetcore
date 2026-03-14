// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Tools.Internal;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.SecretManager.Tools.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public class SecretsStore
{
    private readonly string _secretsFilePath;
    private readonly IDictionary<string, string> _secrets;

    public SecretsStore(string userSecretsId, IReporter reporter)
    {
        Ensure.NotNull(userSecretsId, nameof(userSecretsId));

        _secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);

        // workaround bug in configuration
        var secretDir = Path.GetDirectoryName(_secretsFilePath);
        Directory.CreateDirectory(secretDir);

        reporter.Verbose(Resources.FormatMessage_Secret_File_Path(_secretsFilePath));
        _secrets = Load(userSecretsId);
    }

    public string this[string key]
    {
        get
        {
            return _secrets[key];
        }
    }

    public int Count => _secrets.Count;

    // For testing.
    internal string SecretsFilePath => _secretsFilePath;

    public bool ContainsKey(string key) => _secrets.ContainsKey(key);

    public IEnumerable<KeyValuePair<string, string>> AsEnumerable() => _secrets;

    public void Clear() => _secrets.Clear();

    public void Set(string key, string value) => _secrets[key] = value;

    public void Remove(string key)
    {
        if (_secrets.ContainsKey(key))
        {
            _secrets.Remove(key);
        }
    }

    public virtual void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_secretsFilePath));

        var contents = new JObject();
        if (_secrets != null)
        {
            foreach (var secret in _secrets.AsEnumerable())
            {
                contents[secret.Key] = secret.Value;
            }
        }

        // Create a temp file with the correct Unix file mode before moving it to the expected _filePath.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var tempFilename = Path.GetTempFileName();
            File.Move(tempFilename, _secretsFilePath, overwrite: true);
        }

        File.WriteAllText(_secretsFilePath, contents.ToString(), Encoding.UTF8);
    }

    protected virtual IDictionary<string, string> Load(string userSecretsId)
    {
        var secrets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(_secretsFilePath))
        {
            return secrets;
        }

        var content = File.ReadAllText(_secretsFilePath);
        if (string.IsNullOrWhiteSpace(content))
        {
            return secrets;
        }

        try
        {
            // Use ConfigurationBuilder for standard JSON parsing with flattened keys.
            var config = new ConfigurationBuilder()
                .AddJsonFile(_secretsFilePath, optional: true)
                .Build();
            foreach (var kvp in config.AsEnumerable())
            {
                if (kvp.Value is not null)
                {
                    secrets[kvp.Key] = kvp.Value;
                }
            }
        }
        catch (InvalidDataException)
        {
            // If the file contains case-different duplicate keys, the JSON configuration
            // parser throws. Fall back to parsing the JSON directly and using last-wins
            // semantics for case-insensitive key collisions.
            var jObject = JObject.Parse(content);
            foreach (var property in jObject.Properties())
            {
                if (property.Value.Type == Newtonsoft.Json.Linq.JTokenType.String)
                {
                    secrets[property.Name] = property.Value.ToString();
                }
            }
        }

        return secrets;
    }
}
