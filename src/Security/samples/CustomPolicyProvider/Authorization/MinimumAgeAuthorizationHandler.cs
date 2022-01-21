// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace CustomPolicyProvider;

// This class contains logic for determining whether MinimumAgeRequirements in authorization
// policies are satisfied or not
internal class MinimumAgeAuthorizationHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    private readonly ILogger<MinimumAgeAuthorizationHandler> _logger;

    public MinimumAgeAuthorizationHandler(ILogger<MinimumAgeAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    // Check whether a given MinimumAgeRequirement is satisfied or not for a particular context
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumAgeRequirement requirement)
    {
        // Log as a warning so that it's very clear in sample output which authorization policies
        // (and requirements/handlers) are in use
        _logger.LogWarning("Evaluating authorization requirement for age >= {age}", requirement.Age);

        // Check the user's age
        var dateOfBirthClaim = context.User.FindFirst(c => c.Type == ClaimTypes.DateOfBirth);
        if (dateOfBirthClaim != null)
        {
            // If the user has a date of birth claim, check their age
            var dateOfBirth = Convert.ToDateTime(dateOfBirthClaim.Value, CultureInfo.InvariantCulture);
            var age = DateTime.Now.Year - dateOfBirth.Year;
            if (dateOfBirth > DateTime.Now.AddYears(-age))
            {
                // Adjust age if the user hasn't had a birthday yet this year
                age--;
            }

            // If the user meets the age criterion, mark the authorization requirement succeeded
            if (age >= requirement.Age)
            {
                _logger.LogInformation("Minimum age authorization requirement {age} satisfied", requirement.Age);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogInformation("Current user's DateOfBirth claim ({dateOfBirth}) does not satisfy the minimum age authorization requirement {age}",
                    dateOfBirthClaim.Value,
                    requirement.Age);
            }
        }
        else
        {
            _logger.LogInformation("No DateOfBirth claim present");
        }

        return Task.CompletedTask;
    }
}
