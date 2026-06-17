# Generating template-baselines.json

For small project template changes, you may be able to edit the `template-baselines.json` file manually. This is a good way to ensure you have correct expectations about the effects of your changes.

For larger changes such as adding entirely new templates, it may be impractical to type out the changes to `template-baselines.json` manually. In those cases you can follow a procedure like the following.

  1. Ensure you've configured the necessary environment variables:
        - `set PATH=c:\git\dotnet\aspnetcore\.dotnet\;%PATH%` (update path as needed)
        - `set DOTNET_ROOT=c:\git\dotnet\aspnetcore\.dotnet` (update path as needed)
  2. Get to a position where you can execute the modified template(s) locally, i.e.:
        - Use `dotnet pack ProjectTemplatesNoDeps.slnf` (possibly with `--no-restore --no-dependencies`) to regenerate `Microsoft.DotNet.Web.ProjectTemplates.*.nupkg`
        - Run one of the `scripts/*.ps1` scripts to install your template pack and execute your chosen template. For example, run `powershell .\scripts\Run-BlazorWeb-Locally.ps1`
        - Once that has run, you should see your updated template listed when you execute `dotnet new list` or `dotnet new YourTemplateName --help`. At the point you can run `dotnet new YourTemplateName -o SomePath` directly if you want. However each time you edit template sources further, you will need to run `dotnet new uninstall Microsoft.DotNet.Web.ProjectTemplates.8.0` and then go back to the start of this whole step.
        - Tip: the following command combines the above steps, to go directly from editing template sources to an updated local project output: `dotnet pack ProjectTemplatesNoDeps.slnf --no-restore --no-dependencies && dotnet new uninstall Microsoft.DotNet.Web.ProjectTemplates.8.0 && rm -rf scripts\MyBlazorApp && powershell .\scripts\Run-BlazorWeb-Locally.ps1`
  3. After generating a particular project's output, the following can be run in a Bash prompt (e.g., using WSL):
        - `cd src/ProjectTemplates/scripts`
        - `export PROJECT_NAME=MyBlazorApp` (update as necessary - note this is the name of the directly under `scripts` containing your project output)
        - `find $PROJECT_NAME -type f -not -path "*/obj/*" -not -path "*/bin/*" -not -path "*/.publish/*" | sed -e "s/^$PROJECT_NAME\///" | sed -e "s/$PROJECT_NAME/{ProjectName}/g" | sed 's/.*/        "&",/' | sort -f`
        - This will emit the JSON-formatted lines you can manually insert into the relevant place inside `template-baselines.json`
