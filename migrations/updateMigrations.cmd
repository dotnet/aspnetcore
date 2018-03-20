echo Make sure to have ran build.cmd once to ensure artifacts have been created.
rd "Company.WebApplication1" /s /q
mkdir "Company.WebApplication1"
dotnet new razor --auth Individual -i ..\artifacts\build\Microsoft.DotNet.Web.ProjectTemplates.*.nupkg -o Company.WebApplication1
copy "Company.WebApplication1\Data\Migrations\*" "..\src\Microsoft.DotNet.Web.ProjectTemplates\content\RazorPagesWeb-CSharp\Data\SqlLite"
rd "Company.WebApplication1" /s /q
mkdir "Company.WebApplication1"
dotnet new razor --auth Individual --use-local-db -i ..\artifacts\build\Microsoft.DotNet.Web.ProjectTemplates.*.nupkg -o Company.WebApplication1
copy "Company.WebApplication1\Data\Migrations\*" "..\src\Microsoft.DotNet.Web.ProjectTemplates\content\RazorPagesWeb-CSharp\Data\SqlServer"
rd "Company.WebApplication1" /s /q
mkdir "Company.WebApplication1"
dotnet new mvc --auth Individual --use-local-db -i ..\artifacts\build\Microsoft.DotNet.Web.ProjectTemplates.*.nupkg -o Company.WebApplication1
copy "Company.WebApplication1\Data\Migrations\*" "..\src\Microsoft.DotNet.Web.ProjectTemplates\content\StarterWeb-CSharp\Data\SqlServer"
rd "Company.WebApplication1" /s /q
mkdir "Company.WebApplication1"
dotnet new mvc --auth Individual -i ..\artifacts\build\Microsoft.DotNet.Web.ProjectTemplates.*.nupkg -o Company.WebApplication1
copy "Company.WebApplication1\Data\Migrations\*" "..\src\Microsoft.DotNet.Web.ProjectTemplates\content\StarterWeb-CSharp\Data\SqlLite"
rd "Company.WebApplication1" /s /q
