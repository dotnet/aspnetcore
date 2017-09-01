// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Identity.OpenIdConnect.WebSite.Identity.Models;
using Identity.OpenIdConnect.WebSite.Identity.Models.ApplicationViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Identity.OpenIdConnect.WebSite.Identity.Controllers
{
    [Authorize(IdentityServiceOptions.ManagementPolicyName)]
    [Area("Identity")]
    public class ApplicationsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationManager<IdentityServiceApplication> _applicationManager;

        public ApplicationsController(
            UserManager<ApplicationUser> userManager,
            ApplicationManager<IdentityServiceApplication> applicationManager)
        {
            _userManager = userManager;
            _applicationManager = applicationManager;
        }

        [HttpGet("tfp/Identity/signinsignup/Applications")]
        public async Task<IActionResult> Index()
        {
            var id = _userManager.GetUserId(User);
            var applications = await _applicationManager.Applications.ToListAsync();
            return View(applications);
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/Create")]
        public async Task<IActionResult> Create(CreateApplicationViewModel model)
        {
            var application = new IdentityServiceApplication
            {
                Name = model.Name,
                ClientId = Guid.NewGuid().ToString()
            };

            await _applicationManager.CreateAsync(application);
            await _applicationManager.AddScopeAsync(application, OpenIdConnectScope.OpenId);
            await _applicationManager.AddScopeAsync(application, "offline_access");

            return RedirectToAction(nameof(CreateScope), new { id = application.Id });
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/Scopes/Create")]
        public async Task<IActionResult> CreateScope([FromRoute] string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            var scopes = await _applicationManager.FindScopesAsync(application);

            return View(new CreateScopeViewModel(applicationName, scopes));
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/Scopes/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateScope(
            [FromRoute] string id,
            [FromForm] CreateScopeViewModel model)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            var scopes = await _applicationManager.FindScopesAsync(application);
            if (!ModelState.IsValid)
            {
                return View(new CreateScopeViewModel(applicationName, scopes));
            }

            var result = await _applicationManager.AddScopeAsync(application, model.NewScope);
            if (!result.Succeeded)
            {
                MapErrorsToModelState("", result);
                return View(new CreateScopeViewModel(applicationName, scopes));
            }

            return RedirectToAction(nameof(CreateScope), new { id });
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/RedirectUris/Create")]
        public async Task<IActionResult> CreateRedirectUri([FromRoute] string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            var redirectUris = await _applicationManager.FindRegisteredUrisAsync(application);

            return View(new CreateRedirectUriViewModel(applicationName, redirectUris));
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/RedirectUris/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRedirectUri(
            [FromRoute] string id,
            [FromForm] CreateRedirectUriViewModel model)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            var redirectUris = await _applicationManager.FindRegisteredUrisAsync(application);
            if (!ModelState.IsValid)
            {
                return View(new CreateRedirectUriViewModel(applicationName, redirectUris));
            }

            var result = await _applicationManager.RegisterRedirectUriAsync(application, model.NewRedirectUri);
            if (!result.Succeeded)
            {
                MapErrorsToModelState("", result);
                return View(new CreateRedirectUriViewModel(applicationName, redirectUris));
            }

            return RedirectToAction(nameof(CreateRedirectUri), new { id });
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/LogoutUris/Create")]
        public async Task<IActionResult> CreateLogoutUri([FromRoute] string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            var logoutUris = await _applicationManager.FindRegisteredLogoutUrisAsync(application);

            return View(new CreateLogoutUriViewModel(id, applicationName, logoutUris));
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/LogoutUris/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLogoutUri(
            [FromRoute] string id,
            [FromForm] CreateLogoutUriViewModel model)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            var logoutUris = await _applicationManager.FindRegisteredLogoutUrisAsync(application);
            if (!ModelState.IsValid)
            {
                return View(new CreateLogoutUriViewModel(id, applicationName, logoutUris));
            }

            var result = await _applicationManager.RegisterLogoutUriAsync(application, model.NewLogoutUri);
            if (!result.Succeeded)
            {
                MapErrorsToModelState("", result);
                return View(new CreateLogoutUriViewModel(id, applicationName, logoutUris));
            }

            return RedirectToAction(nameof(CreateLogoutUri), new { id });
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/Remove")]
        public async Task<IActionResult> RemoveApplication([FromRoute] string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);

            return View(new RemoveApplicationViewModel(applicationName));
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/Remove")]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(RemoveApplicationViewModel))]
        public async Task<IActionResult> RemoveApplicationConfirmed([FromRoute] string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            if (!ModelState.IsValid)
            {
                return View(new RemoveApplicationViewModel(applicationName));
            }

            var result = await _applicationManager.DeleteAsync(application);
            if (!result.Succeeded)
            {
                MapErrorsToModelState("", result);
                return View(new RemoveApplicationViewModel(applicationName));
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}")]
        public async Task<IActionResult> Details([FromRoute]string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            var applicationClientId = await _applicationManager.GetApplicationClientIdAsync(application);
            var hasClientSecret = await _applicationManager.HasClientSecretAsync(application);
            var redirectUris = await _applicationManager.FindRegisteredUrisAsync(application);
            var logoutUris = await _applicationManager.FindRegisteredLogoutUrisAsync(application);
            var scopes = await _applicationManager.FindScopesAsync(application);

            return View(new ApplicationDetailsViewModel
            {
                Name = applicationName,
                ClientId = applicationClientId,
                HasClientSecret = hasClientSecret,
                RedirectUris = redirectUris,
                LogoutUris = logoutUris,
                Scopes = scopes
            });
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/ChangeName")]
        public async Task<IActionResult> ChangeName([FromRoute]string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);

            return View(new ChangeApplicationNameViewModel(applicationName));
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/ChangeName")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeName([FromRoute]string id, [FromForm] ChangeApplicationNameViewModel model)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            if (!ModelState.IsValid)
            {
                return View(new ChangeApplicationNameViewModel(applicationName));
            }

            var changeNameResult = await _applicationManager.SetApplicationNameAsync(application, model.Name);
            if (!changeNameResult.Succeeded)
            {
                MapErrorsToModelState("", changeNameResult);
                return View(new ChangeApplicationNameViewModel(applicationName));
            }

            return RedirectToAction(nameof(Details), new { id });
        }


        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/GenerateClientSecret")]
        public async Task<IActionResult> GenerateClientSecret([FromRoute]string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var name = await _applicationManager.GetApplicationNameAsync(application);
            return View(model: name);
        }


        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/GenerateClientSecret")]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(GenerateClientSecret))]
        public async Task<IActionResult> GenerateClientSecretConfirmed([FromRoute]string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var name = await _applicationManager.GetApplicationNameAsync(application);

            var clientSecret = await _applicationManager.GenerateClientSecretAsync();
            var addSecretResult = await _applicationManager.AddClientSecretAsync(application, clientSecret);
            if (!addSecretResult.Succeeded)
            {
                MapErrorsToModelState("", addSecretResult);
                return View(model: name);
            }

            return View("GeneratedClientSecret", new GeneratedClientSecretViewModel(name, clientSecret));
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/RemoveClientSecret")]
        public async Task<IActionResult> RemoveClientSecret([FromRoute]string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var name = await _applicationManager.GetApplicationNameAsync(application);
            return View(model: name);
        }


        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/RemoveClientSecret")]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(RemoveClientSecret))]
        public async Task<IActionResult> RemoveClientSecretConfirmed([FromRoute]string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var name = await _applicationManager.GetApplicationNameAsync(application);

            var removeSecretResult = await _applicationManager.RemoveClientSecretAsync(application);
            if (!removeSecretResult.Succeeded)
            {
                MapErrorsToModelState("", removeSecretResult);
                return View(model: name);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/RegenerateClientSecret")]
        public async Task<IActionResult> RegenerateClientSecret([FromRoute]string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var name = await _applicationManager.GetApplicationNameAsync(application);
            return View(model: name);
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/RegenerateClientSecret")]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(RegenerateClientSecret))]
        public async Task<IActionResult> RegenerateClientSecretConfirmed([FromRoute]string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var name = await _applicationManager.GetApplicationNameAsync(application);
            var clientSecret = await _applicationManager.GenerateClientSecretAsync();
            var changeSecretResult = await _applicationManager.ChangeClientSecretAsync(application, clientSecret);
            if (!changeSecretResult.Succeeded)
            {
                MapErrorsToModelState("", changeSecretResult);
                return View(model: name);
            }

            return View("GeneratedClientSecret", new GeneratedClientSecretViewModel(name, clientSecret));
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/Scopes/Add")]
        public async Task<IActionResult> AddScope([FromRoute] string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var scopes = await _applicationManager.FindScopesAsync(application);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);

            return View(new CreateScopeViewModel(applicationName, scopes));
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/Scopes/Add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddScope(
            [FromRoute] string id,
            [FromForm] CreateScopeViewModel model)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var scopes = await _applicationManager.FindScopesAsync(application);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            if (!ModelState.IsValid)
            {
                return View(new CreateScopeViewModel(applicationName, scopes));
            }

            var result = await _applicationManager.AddScopeAsync(application, model.NewScope);
            if (!result.Succeeded)
            {
                MapErrorsToModelState("", result);
                return View(new CreateScopeViewModel(applicationName, scopes));
            }

            return RedirectToAction(nameof(AddScope), new { id });
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/Scopes/Edit/{scope}")]
        public async Task<IActionResult> EditScope([FromRoute] string id, [FromRoute] string scope)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);

            return View(new EditScopeViewModel(applicationName, scope));
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/Scopes/Edit/{scope}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditScope(
            [FromRoute] string id,
            [FromRoute] string scope,
            [FromForm] EditScopeViewModel model)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            if (!ModelState.IsValid)
            {
                return View(new EditScopeViewModel(applicationName, scope));
            }

            var result = await _applicationManager.UpdateScopeAsync(application, scope, model.Scope);
            if (!result.Succeeded)
            {
                MapErrorsToModelState("", result);
                return View(new EditScopeViewModel(applicationName, scope));
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/RedirectUri/Edit")]
        public async Task<IActionResult> EditRedirectUri([FromRoute] string id, [FromQuery] string redirectUri)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);

            return View(new EditRedirectUriViewModel(applicationName, redirectUri));
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/RedirectUri/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRedirectUri(
            [FromRoute] string id,
            [FromQuery] string redirectUri,
            [FromForm] EditRedirectUriViewModel model)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            if (!ModelState.IsValid)
            {
                return View(new EditRedirectUriViewModel(applicationName, redirectUri));
            }

            var result = await _applicationManager.UpdateRedirectUriAsync(application, redirectUri, model.RedirectUri);
            if (!result.Succeeded)
            {
                MapErrorsToModelState("", result);
                return View(new EditRedirectUriViewModel(applicationName, redirectUri));
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/LogoutUri/Edit")]
        public async Task<IActionResult> EditLogoutUri([FromRoute] string id, [FromQuery] string logoutUri)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);

            return View(new EditLogoutUriViewModel(applicationName, logoutUri));
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/LogoutUri/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLogoutUri(
            [FromRoute] string id,
            [FromQuery] string logoutUri,
            [FromForm] EditLogoutUriViewModel model)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            if (!ModelState.IsValid)
            {
                return View(new EditLogoutUriViewModel(applicationName, logoutUri));
            }

            var result = await _applicationManager.UpdateLogoutUriAsync(application, logoutUri, model.LogoutUri);
            if (!result.Succeeded)
            {
                MapErrorsToModelState("", result);
                return View(new EditLogoutUriViewModel(applicationName, logoutUri));
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/Scopes/Remove/{scope}")]
        public async Task<IActionResult> RemoveScope([FromRoute] string id, [FromRoute] string scope)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);

            return View(new RemoveScopeViewModel(applicationName, scope));
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/Scopes/Remove/{scope}")]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(RemoveScope))]
        public async Task<IActionResult> RemoveScopeConfirmed([FromRoute] string id, [FromRoute] string scope)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            if (!ModelState.IsValid)
            {
                return View(new RemoveScopeViewModel(applicationName, scope));
            }

            var result = await _applicationManager.RemoveScopeAsync(application, scope);
            if (!result.Succeeded)
            {
                MapErrorsToModelState("", result);
                return View(new RemoveScopeViewModel(applicationName, scope));
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/RedirectUri/Remove")]
        public async Task<IActionResult> RemoveRedirectUri([FromRoute] string id, [FromQuery] string redirectUri)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);

            return View(new RemoveRedirectUriViewModel(applicationName, redirectUri));
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/RedirectUri/Remove")]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(RemoveRedirectUri))]
        public async Task<IActionResult> RemoveRedirectUriConfirmed([FromRoute] string id, [FromQuery] string redirectUri)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            if (!ModelState.IsValid)
            {
                return View(new RemoveRedirectUriViewModel(applicationName, redirectUri));
            }

            var result = await _applicationManager.UnregisterRedirectUriAsync(application, redirectUri);
            if (!result.Succeeded)
            {
                MapErrorsToModelState("", result);
                return View(new RemoveRedirectUriViewModel(applicationName, redirectUri));
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/LogoutUri/Remove")]
        public async Task<IActionResult> RemoveLogoutUri([FromRoute] string id, [FromQuery] string logoutUri)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);

            return View(new RemoveLogoutUriViewModel(applicationName, logoutUri));
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/LogoutUri/Remove")]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(RemoveLogoutUri))]
        public async Task<IActionResult> RemoveLogoutUriConfirmed([FromRoute] string id, [FromQuery] string logoutUri)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            if (!ModelState.IsValid)
            {
                return View(new RemoveLogoutUriViewModel(applicationName, logoutUri));
            }

            var result = await _applicationManager.UnregisterLogoutUriAsync(application, logoutUri);
            if (!result.Succeeded)
            {
                MapErrorsToModelState("", result);
                return View(new RemoveLogoutUriViewModel(applicationName, logoutUri));
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/RedirectUris/Add")]
        public async Task<IActionResult> AddRedirectUri([FromRoute] string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var redirectUris = await _applicationManager.FindRegisteredUrisAsync(application);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);

            return View(new CreateRedirectUriViewModel(applicationName, redirectUris));
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/RedirectUris/Add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRedirectUri(
            [FromRoute] string id,
            [FromForm] CreateRedirectUriViewModel model)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var redirectUris = await _applicationManager.FindRegisteredUrisAsync(application);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            if (!ModelState.IsValid)
            {
                return View(new CreateRedirectUriViewModel(applicationName, redirectUris));
            }

            var result = await _applicationManager.RegisterRedirectUriAsync(application, model.NewRedirectUri);
            if (!result.Succeeded)
            {
                MapErrorsToModelState("", result);
                return View(new CreateRedirectUriViewModel(applicationName, redirectUris));
            }

            return RedirectToAction(nameof(AddRedirectUri), new { id });
        }

        [HttpGet("tfp/Identity/signinsignup/Applications/{id}/LogoutUris/Add")]
        public async Task<IActionResult> AddLogoutUri([FromRoute] string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var logoutUris = await _applicationManager.FindRegisteredLogoutUrisAsync(application);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);

            return View(new CreateLogoutUriViewModel(id, applicationName, logoutUris));
        }

        [HttpPost("tfp/Identity/signinsignup/Applications/{id}/LogoutUris/Add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLogoutUri(
            [FromRoute] string id,
            [FromForm] CreateLogoutUriViewModel model)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var logoutUris = await _applicationManager.FindRegisteredLogoutUrisAsync(application);
            var applicationName = await _applicationManager.GetApplicationNameAsync(application);
            if (!ModelState.IsValid)
            {
                return View(new CreateLogoutUriViewModel(id, applicationName, logoutUris));
            }

            var result = await _applicationManager.RegisterLogoutUriAsync(application, model.NewLogoutUri);
            if (!result.Succeeded)
            {
                MapErrorsToModelState("", result);
                return View(new CreateLogoutUriViewModel(id, applicationName, logoutUris));
            }

            return RedirectToAction(nameof(AddLogoutUri), new { id });
        }

        private void MapErrorsToModelState(string key, IdentityServiceResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(key, error.Description);
            }
        }
    }
}
