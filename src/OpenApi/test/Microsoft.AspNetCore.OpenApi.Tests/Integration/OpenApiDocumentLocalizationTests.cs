// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

[UsesVerify]
public sealed class OpenApiDocumentLocalizationTests(LocalizedSampleAppFixture fixture)
    : OpenApiDocumentIntegrationTests(fixture), IClassFixture<LocalizedSampleAppFixture>;
