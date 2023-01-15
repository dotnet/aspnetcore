// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Templates.Test.Helpers;

internal static class ArgConstants
{
    public const string UseProgramMain = "--use-program-main";
    public const string UseMinimalApis = "--use-minimal-apis";
    public const string Hosted = "--hosted";
    public const string Pwa = "--pwa";
    public const string CallsGraph = "--calls-graph";
    public const string CalledApiUrl = "--called-api-url";
    public const string CalledApiUrlGraphMicrosoftCom = "--called-api-url \"https://graph.microsoft.com\"";
    public const string CalledApiScopes = "--called-api-scopes";
    public const string CalledApiScopesUserReadWrite = $"{CalledApiScopes} user.readwrite";
    public const string NoOpenApi = "--no-openapi";
    public const string Auth = "-au";
    public const string ClientId = "--client-id";
    public const string Domain = "--domain";
    public const string DefaultScope = "--default-scope";
    public const string AppIdUri = "--app-id-uri";
    public const string AppIdClientId = "--api-client-id";
    public const string TenantId = "--tenant-id";
    public const string AadB2cInstance = "--aad-b2c-instance";
    public const string UseLocalDb = "-uld";
    public const string NoHttps = "--no-https";
    public const string PublishNativeAot = "--publish-native-aot";
}
