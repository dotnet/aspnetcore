// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

public class JwtStore
{
    private const string FileName = "user-jwts.json";
    private readonly string _filePath;

    public JwtStore(string userSecretsId, Program program = null)
    {
        _filePath = Path.Combine(Path.GetDirectoryName(PathHelper.GetSecretsPathFromSecretsId(userSecretsId)), FileName);
        Load();

        // For testing.
        if (program is not null)
        {
            program.UserJwtsFilePath = _filePath;
        }
    }

    public IDictionary<string, Jwt> Jwts { get; private set; } = new Dictionary<string, Jwt>();

    public void Load()
    {
        if (File.Exists(_filePath))
        {
            using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
            if (fileStream.Length > 0)
            {
                Jwts = JsonSerializer.Deserialize<IDictionary<string, Jwt>>(fileStream, JwtSerializerOptions.Default) ?? new Dictionary<string, Jwt>();
            }
        }
    }

    public void Save()
    {
        if (Jwts is not null)
        {
            // Create a temp file with the correct Unix file mode before moving it to the expected _filePath.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var tempFilename = Path.GetTempFileName();
                File.Move(tempFilename, _filePath, overwrite: true);
            }

            using var fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fileStream, Jwts);
        }
    }
}
