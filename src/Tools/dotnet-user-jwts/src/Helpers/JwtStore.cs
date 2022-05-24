// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

public class JwtStore
{
    private const string FileName = "dev-jwts.json";
    private readonly string _userSecretsId;
    private readonly string _filePath;

    public JwtStore(string userSecretsId)
    {
        _userSecretsId = userSecretsId;
        _filePath = Path.Combine(Path.GetDirectoryName(PathHelper.GetSecretsPathFromSecretsId(userSecretsId)), FileName);
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
                Jwts = JsonSerializer.Deserialize<IDictionary<string, Jwt>>(fileStream) ?? new Dictionary<string, Jwt>();
            }
        }
    }

    public void Save()
    {
        if (Jwts is not null)
        {
            using var fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write);
            JsonSerializer.Serialize(fileStream, Jwts);
        }
    }
}
