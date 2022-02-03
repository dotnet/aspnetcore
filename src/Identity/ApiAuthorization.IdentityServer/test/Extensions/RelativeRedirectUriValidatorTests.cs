// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer;

public class RelativeRedirectUriValidatorTests
{
    [Fact]
    public async Task IsRedirectUriValidAsync_ConvertsRelativeUrisIntoAbsoluteUris_ForLocalSPAsAsync()
    {
        // Arrange
        var expectedRelativeUri = "/authenticate";
        var providedFullUrl = "https://localhost:5001/authenticate";
        var expectedClient = new Client
        {
            RedirectUris = { expectedRelativeUri },
            Properties = new Dictionary<string, string>
            {
                [ApplicationProfilesPropertyNames.Profile] = ApplicationProfiles.IdentityServerSPA,
            }
        };
        var factory = new TestUrlFactory(expectedRelativeUri, providedFullUrl);
        var redirectUriValidator = new RelativeRedirectUriValidator(factory);

        // Act
        var validator = await redirectUriValidator.IsRedirectUriValidAsync(providedFullUrl, expectedClient);

        // Assert
        Assert.True(validator);
    }

    [Fact]
    public async Task IsRedirectUriValidAsync_RejectsIfTheRelativeUriIsNotRegistered_ForLocalSPAsAsync()
    {
        // Arrange
        var expectedRelativeUri = "/authenticate";
        var providedFullUrl = "https://localhost:5001/notregistered";
        var expectedClient = new Client
        {
            RedirectUris = { expectedRelativeUri },
            Properties = new Dictionary<string, string>
            {
                [ApplicationProfilesPropertyNames.Profile] = ApplicationProfiles.IdentityServerSPA,
            }
        };
        var factory = new TestUrlFactory();
        var redirectUriValidator = new RelativeRedirectUriValidator(factory);

        // Act
        var validator = await redirectUriValidator.IsRedirectUriValidAsync(providedFullUrl, expectedClient);

        // Assert
        Assert.False(validator);
    }

    [Fact]
    public async Task IsRedirectUriValidAsync_CallsBaseAndSucceeds_ForValidRedirectUrisOnRegularClients()
    {
        // Arrange
        var providedFullUrl = "https://localhost:5001/authenticate";
        var expectedClient = new Client
        {
            RedirectUris = { "https://localhost:5001/authenticate" },
        };

        var factory = new TestUrlFactory();
        var redirectUriValidator = new RelativeRedirectUriValidator(factory);

        // Act
        var validator = await redirectUriValidator.IsRedirectUriValidAsync(providedFullUrl, expectedClient);

        // Assert
        Assert.True(validator);
    }

    [Fact]
    public async Task IsRedirectUriValidAsync_CallsBaseAndFails_ForInvalidRedirectUrisOnRegularClients()
    {
        // Arrange
        var providedFullUrl = "https://localhost:5001/notregistered";
        var expectedClient = new Client
        {
            RedirectUris = { "https://localhost:5001/authenticate" },
        };

        var factory = new TestUrlFactory();
        var redirectUriValidator = new RelativeRedirectUriValidator(factory);

        // Act
        var validator = await redirectUriValidator.IsRedirectUriValidAsync(providedFullUrl, expectedClient);

        // Assert
        Assert.False(validator);
    }

    [Fact]
    public async Task IsPostLogoutRedirectUriValidAsync_ConvertsRelativeUrisIntoAbsoluteUris_ForLocalSPAsAsync()
    {
        // Arrange
        var expectedRelativeUri = "/logout";
        var providedFullUrl = "https://localhost:5001/logout";
        var expectedClient = new Client
        {
            PostLogoutRedirectUris = { expectedRelativeUri },
            Properties = new Dictionary<string, string>
            {
                [ApplicationProfilesPropertyNames.Profile] = ApplicationProfiles.IdentityServerSPA,
            }
        };
        var factory = new TestUrlFactory(expectedRelativeUri, providedFullUrl);
        var redirectUriValidator = new RelativeRedirectUriValidator(factory);

        // Act
        var validator = await redirectUriValidator.IsPostLogoutRedirectUriValidAsync(providedFullUrl, expectedClient);

        // Assert
        Assert.True(validator);
    }

    [Fact]
    public async Task IsPostLogoutRedirectUriValidAsync_RejectsIfTheRelativeUriIsNotRegistered_ForLocalSPAsAsync()
    {
        // Arrange
        var expectedRelativeUri = "/logout";
        var providedFullUrl = "https://localhost:5001/notregistered";
        var expectedClient = new Client
        {
            PostLogoutRedirectUris = { expectedRelativeUri },
            Properties = new Dictionary<string, string>
            {
                [ApplicationProfilesPropertyNames.Profile] = ApplicationProfiles.IdentityServerSPA,
            }
        };
        var factory = new TestUrlFactory();
        var redirectUriValidator = new RelativeRedirectUriValidator(factory);

        // Act
        var validator = await redirectUriValidator.IsPostLogoutRedirectUriValidAsync(providedFullUrl, expectedClient);

        // Assert
        Assert.False(validator);
    }

    [Fact]
    public async Task IsPostLogoutRedirectUriValidAsync_CallsBaseAndSucceeds_ForValidPostLogoutRedirectUrisOnRegularClients()
    {
        // Arrange
        var providedFullUrl = "https://localhost:5001/logout";
        var expectedClient = new Client
        {
            PostLogoutRedirectUris = { "https://localhost:5001/logout" },
        };

        var factory = new TestUrlFactory();
        var redirectUriValidator = new RelativeRedirectUriValidator(factory);

        // Act
        var validator = await redirectUriValidator.IsPostLogoutRedirectUriValidAsync(providedFullUrl, expectedClient);

        // Assert
        Assert.True(validator);
    }

    [Fact]
    public async Task IsPostLogoutRedirectUriValidAsync_CallsBaseAndFails_ForInvalidPostLogoutRedirectUrisOnRegularClients()
    {
        // Arrange
        var providedFullUrl = "https://localhost:5001/notregistered";
        var expectedClient = new Client
        {
            PostLogoutRedirectUris = { "https://localhost:5001/logout" },
        };

        var factory = new TestUrlFactory();
        var redirectUriValidator = new RelativeRedirectUriValidator(factory);

        // Act
        var validator = await redirectUriValidator.IsPostLogoutRedirectUriValidAsync(providedFullUrl, expectedClient);

        // Assert
        Assert.False(validator);
    }

    private class TestUrlFactory : IAbsoluteUrlFactory
    {
        private readonly string _path;
        private readonly string _result;

        public TestUrlFactory()
        {
        }

        public TestUrlFactory(string path, string result)
        {
            _path = path;
            _result = result;
        }

        public string GetAbsoluteUrl(string path)
        {
            if (_path == null || _result == null)
            {
                return null;
            }

            if (_path == path)
            {
                return _result;
            }

            return path;
        }

        public string GetAbsoluteUrl(HttpContext context, string path)
        {
            return GetAbsoluteUrl(path);
        }
    }
}
