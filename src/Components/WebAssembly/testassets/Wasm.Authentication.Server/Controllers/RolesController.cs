// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Wasm.Authentication.Server.Models;

namespace Wasm.Authentication.Server.Controllers;

[Authorize]
public class RolesController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IOptions<IdentityOptions> _options;

    public RolesController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _options = options;
    }

    [HttpPost("[controller]/[action]")]
    public async Task<IActionResult> MakeAdmin()
    {
        var admin = await _roleManager.FindByNameAsync("admin");
        if (admin == null)
        {
            await _roleManager.CreateAsync(new IdentityRole { Name = "admin" });
        }

        var id = User.FindFirst(ClaimTypes.NameIdentifier);
        if (id == null)
        {
            return BadRequest();
        }
        var currentUser = await _userManager.FindByIdAsync(id.Value);
        await _userManager.AddToRoleAsync(currentUser, "admin");

        return Ok();
    }

    [HttpPost("[controller]/[action]")]
    [Authorize(Roles = "admin")]
    public IActionResult AdminOnly()
    {
        return Ok();
    }
}
