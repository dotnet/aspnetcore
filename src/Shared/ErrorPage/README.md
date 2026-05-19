# Error Page

Multiple projects share this folder. The `ErrorPage.Designer.cs` and `ErrorPageModel.cs` files render this shared
view. The [RazorPageGenerator](/src/Razor/tools/RazorSyntaxGenerator/) tool generates the `ErrorPage.Designer.cs`
file from [`Views/ErrorPage.cshtml`](Views/ErrorPage.cshtml).

## Making changes to ErrorPage.cshtml

1. Edit the [`Views/ErrorPage.cshtml`](Views/ErrorPage.cshtml) file.
1. Run the [`GeneratePage`](GeneratePage.ps1) script.

``` powershell
.\GeneratePage.ps1
```
