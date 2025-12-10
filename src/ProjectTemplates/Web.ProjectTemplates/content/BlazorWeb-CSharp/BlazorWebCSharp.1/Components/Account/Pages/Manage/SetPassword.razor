@page "/Account/Manage/SetPassword"

@using System.ComponentModel.DataAnnotations
@using Microsoft.AspNetCore.Identity
@using BlazorWebCSharp._1.Data

@inject UserManager<ApplicationUser> UserManager
@inject SignInManager<ApplicationUser> SignInManager
@inject IdentityRedirectManager RedirectManager

<PageTitle>Set password</PageTitle>

<h3>Set your password</h3>
<StatusMessage Message="@message" />
<p class="text-info">
    You do not have a local username/password for this site. Add a local
    account so you can log in without an external login.
</p>
<div class="row">
    <div class="col-xl-6">
        <EditForm Model="Input" FormName="set-password" OnValidSubmit="OnValidSubmitAsync" method="post">
            <DataAnnotationsValidator />
            <ValidationSummary class="text-danger" role="alert" />
            <div class="form-floating mb-3">
                <InputText type="password" @bind-Value="Input.NewPassword" id="Input.NewPassword" class="form-control" autocomplete="new-password" placeholder="Enter the new password" />
                <label for="Input.NewPassword" class="form-label">New password</label>
                <ValidationMessage For="() => Input.NewPassword" class="text-danger" />
            </div>
            <div class="form-floating mb-3">
                <InputText type="password" @bind-Value="Input.ConfirmPassword" id="Input.ConfirmPassword" class="form-control" autocomplete="new-password" placeholder="Enter the new password" />
                <label for="Input.ConfirmPassword" class="form-label">Confirm password</label>
                <ValidationMessage For="() => Input.ConfirmPassword" class="text-danger" />
            </div>
            <button type="submit" class="w-100 btn btn-lg btn-primary">Set password</button>
        </EditForm>
     </div>
</div>

@code {
    private string? message;
    private ApplicationUser? user;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        user = await UserManager.GetUserAsync(HttpContext.User);
        if (user is null)
        {
            RedirectManager.RedirectToInvalidUser(UserManager, HttpContext);
            return;
        }

        var hasPassword = await UserManager.HasPasswordAsync(user);
        if (hasPassword)
        {
            RedirectManager.RedirectTo("Account/Manage/ChangePassword");
        }
    }

    private async Task OnValidSubmitAsync()
    {
        if (user is null)
        {
            RedirectManager.RedirectToInvalidUser(UserManager, HttpContext);
            return;
        }

        var addPasswordResult = await UserManager.AddPasswordAsync(user, Input.NewPassword!);
        if (!addPasswordResult.Succeeded)
        {
            message = $"Error: {string.Join(",", addPasswordResult.Errors.Select(error => error.Description))}";
            return;
        }

        await SignInManager.RefreshSignInAsync(user);
        RedirectManager.RedirectToCurrentPageWithStatus("Your password has been set.", HttpContext);
    }

    private sealed class InputModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}
