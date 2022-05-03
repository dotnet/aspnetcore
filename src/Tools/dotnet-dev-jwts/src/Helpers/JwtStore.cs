// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text.Json;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

public class JwtStore
{
    private const string FileName = "dev-jwts.json";
    private readonly string _userSecretsId;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonSerializerOptions = JsonSerializerOptions.Default;

    public JwtStore(string userSecretsId)
    {
        _userSecretsId = userSecretsId;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets", _userSecretsId, FileName);
        }
        else
        {
<<<<<<< HEAD
            _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "microsoft", "usersecrets", _userSecretsId, FileName);
=======
            _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".microsoft", "usersecrets", _userSecretsId, FileName);
>>>>>>> aed8a228a7 (Add dotnet dev-jwts tool)
        }
        Load();
    }

    public IDictionary<string, Jwt> Jwts { get; private set; } = new Dictionary<string, Jwt>();

    public void Load()
    {
        if (File.Exists(_filePath))
        {
            using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
            if (fileStream.Length > 0)
            {
                Jwts = JsonSerializer.Deserialize<IDictionary<string, Jwt>>(fileStream, _jsonSerializerOptions) ?? new Dictionary<string, Jwt>();
            }
        }
    }

    public void Save()
    {
        if (Jwts is not null)
        {
            using var fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fileStream, Jwts, _jsonSerializerOptions);
        }
    }
}
