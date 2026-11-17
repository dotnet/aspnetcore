// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TestApp.Data;

namespace TestApp;

internal static class CanonicalEmailTest
{
    internal static void AddServices(IServiceCollection services)
    {
        services.RemoveAll<IEmailSender<ApplicationUser>>();
        services.AddSingleton<TestEmailSender>();
        services.AddSingleton<IEmailSender<ApplicationUser>>(services => services.GetRequiredService<TestEmailSender>());
    }

    internal static void MapEndpoints(WebApplication app)
    {
        app.MapPost("/test/canonical-email/users", async (
            TestUserRequest request,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext dbContext) =>
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = request.Confirmed,
            };
            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return Results.BadRequest(result.Errors);
            }

            if (request.ClearStoredEmail)
            {
                user.Email = null;
                await dbContext.SaveChangesAsync();
            }

            return Results.Ok();
        });
        app.MapGet("/test/canonical-email/messages", (TestEmailSender sender) => sender.Addresses);
    }

    internal sealed class TestEmailSender : IEmailSender<ApplicationUser>
    {
        private readonly object _lock = new();
        private readonly List<string> _addresses = [];

        public string[] Addresses
        {
            get
            {
                lock (_lock)
                {
                    return [.. _addresses];
                }
            }
        }

        public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        {
            Add(email);
            return Task.CompletedTask;
        }

        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        {
            Add(email);
            return Task.CompletedTask;
        }

        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        {
            Add(email);
            return Task.CompletedTask;
        }

        private void Add(string email)
        {
            lock (_lock)
            {
                _addresses.Add(email);
            }
        }
    }
}

internal sealed record TestUserRequest(string Email, bool Confirmed, bool ClearStoredEmail);
