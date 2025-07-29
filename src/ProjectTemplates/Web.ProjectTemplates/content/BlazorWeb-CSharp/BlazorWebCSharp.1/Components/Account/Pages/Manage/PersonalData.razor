@page "/Account/Manage/PersonalData"

@using Microsoft.AspNetCore.Identity
@using BlazorWebCSharp._1.Data

@inject UserManager<ApplicationUser> UserManager
@inject IdentityRedirectManager RedirectManager

<PageTitle>Personal Data</PageTitle>

<StatusMessage />
<h3>Personal Data</h3>

<div class="row">
    <div class="col-md-6">
        <p>Your account contains personal data that you have given us. This page allows you to download or delete that data.</p>
        <p>
            <strong>Deleting this data will permanently remove your account, and this cannot be recovered.</strong>
        </p>
        <form action="Account/Manage/DownloadPersonalData" method="post">
            <AntiforgeryToken />
            <button class="btn btn-primary" type="submit">Download</button>
        </form>
        <p>
            <a href="Account/Manage/DeletePersonalData" class="btn btn-danger">Delete</a>
        </p>
    </div>
</div>

@code {
    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var user  = await UserManager.GetUserAsync(HttpContext.User);
        if (user is null)
        {
            RedirectManager.RedirectToInvalidUser(UserManager, HttpContext);
        }
    }
}
