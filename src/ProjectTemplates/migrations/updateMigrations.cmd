SET version=3.0.0-alpha1
echo Make sure to have ran build.cmd once to ensure artifacts have been created.
echo Generating version=%version%, edit updateMigrations.cmd to update the version.

echo Generating migration for RazorPages-SqlLite--------------
rd "Company.WebApplication1" /s /q
mkdir "Company.WebApplication1"
dotnet new razor --no-restore --auth Individual -i ..\artifacts\build\Microsoft.DotNet.Web.ProjectTemplates.*.nupkg -o Company.WebApplication1
rd "Company.WebApplication1\Data\Migrations" /s /q
powershell.exe -File .\updateProj.ps1
cd Company.WebApplication1
dotnet ef migrations add CreateIdentitySchema -o Data\Migrations
cd ..
move Company.WebApplication1\Data\Migrations\*.Designer.cs Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs
move Company.WebApplication1\Data\Migrations\*_CreateIdentitySchema.cs Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.cs
powershell.exe -command "(Get-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs) -replace('([0-9]{14})_CreateIdentitySchema', '00000000000000_CreateIdentitySchema') | Set-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs"
powershell.exe -command "(Get-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs) -replace('[0-9].[0-9].[0-9]-[^-]*-[0-9]*', '%version%') | Set-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs"
powershell.exe -command "(Get-Content Company.WebApplication1\Data\Migrations\ApplicationDbContextModelSnapshot.cs) -replace('[0-9].[0-9].[0-9]-[^-]*-[0-9]*', '%version%') | Set-Content Company.WebApplication1\Data\Migrations\ApplicationDbContextModelSnapshot.cs"
copy "Company.WebApplication1\Data\Migrations\*" "..\src\Microsoft.DotNet.Web.ProjectTemplates\content\RazorPagesWeb-CSharp\Data\SqlLite"

echo Generating migration for RazorPages-SqlServer(localb)--------------
rd "Company.WebApplication1" /s /q
mkdir "Company.WebApplication1"
dotnet new razor --no-restore --auth Individual --use-local-db -i ..\artifacts\build\Microsoft.DotNet.Web.ProjectTemplates.*.nupkg -o Company.WebApplication1
rd "Company.WebApplication1\Data\Migrations" /s /q
powershell.exe -File .\updateProj.ps1
cd Company.WebApplication1
dotnet ef migrations add CreateIdentitySchema -o Data\Migrations
cd ..
move Company.WebApplication1\Data\Migrations\*.Designer.cs Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs
move Company.WebApplication1\Data\Migrations\*_CreateIdentitySchema.cs Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.cs
powershell.exe -command "(Get-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs) -replace('([0-9]{14})_CreateIdentitySchema', '00000000000000_CreateIdentitySchema') | Set-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs"
powershell.exe -command "(Get-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs) -replace('[0-9].[0-9].[0-9]-[^-]*-[0-9]*', '%version%') | Set-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs"
powershell.exe -command "(Get-Content Company.WebApplication1\Data\Migrations\ApplicationDbContextModelSnapshot.cs) -replace('[0-9].[0-9].[0-9]-[^-]*-[0-9]*', '%version%') | Set-Content Company.WebApplication1\Data\Migrations\ApplicationDbContextModelSnapshot.cs"
copy "Company.WebApplication1\Data\Migrations\*" "..\src\Microsoft.DotNet.Web.ProjectTemplates\content\RazorPagesWeb-CSharp\Data\SqlServer"
rd "Company.WebApplication1" /s /q

echo Generating migration for StarterWeb-SqlServer(localb)--------------
mkdir "Company.WebApplication1"
dotnet new mvc --no-restore --auth Individual --use-local-db -i ..\artifacts\build\Microsoft.DotNet.Web.ProjectTemplates.*.nupkg -o Company.WebApplication1
rd "Company.WebApplication1\Data\Migrations" /s /q
powershell.exe -File .\updateProj.ps1
cd Company.WebApplication1
dotnet ef migrations add CreateIdentitySchema -o Data\Migrations
cd ..
move Company.WebApplication1\Data\Migrations\*.Designer.cs Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs
move Company.WebApplication1\Data\Migrations\*_CreateIdentitySchema.cs Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.cs
powershell.exe -command "(Get-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs) -replace('([0-9]{14})_CreateIdentitySchema', '00000000000000_CreateIdentitySchema') | Set-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs"
powershell.exe -command "(Get-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs) -replace('[0-9].[0-9].[0-9]-[^-]*-[0-9]*', '%version%') | Set-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs"
powershell.exe -command "(Get-Content Company.WebApplication1\Data\Migrations\ApplicationDbContextModelSnapshot.cs) -replace('[0-9].[0-9].[0-9]-[^-]*-[0-9]*', '%version%') | Set-Content Company.WebApplication1\Data\Migrations\ApplicationDbContextModelSnapshot.cs"
copy "Company.WebApplication1\Data\Migrations\*" "..\src\Microsoft.DotNet.Web.ProjectTemplates\content\StarterWeb-CSharp\Data\SqlServer"

echo Generating migration for StarterWeb-SqlLite--------------
rd "Company.WebApplication1" /s /q
mkdir "Company.WebApplication1"
dotnet new mvc --no-restore --auth Individual -i ..\artifacts\build\Microsoft.DotNet.Web.ProjectTemplates.*.nupkg -o Company.WebApplication1
rd "Company.WebApplication1\Data\Migrations" /s /q
powershell.exe -File .\updateProj.ps1
cd Company.WebApplication1
dotnet ef migrations add CreateIdentitySchema -o Data\Migrations
cd ..
move Company.WebApplication1\Data\Migrations\*.Designer.cs Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs
move Company.WebApplication1\Data\Migrations\*_CreateIdentitySchema.cs Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.cs
powershell.exe -command "(Get-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs) -replace('([0-9]{14})_CreateIdentitySchema', '00000000000000_CreateIdentitySchema') | Set-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs"
powershell.exe -command "(Get-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs) -replace('[0-9].[0-9].[0-9]-[^-]*-[0-9]*', '%version%') | Set-Content Company.WebApplication1\Data\Migrations\00000000000000_CreateIdentitySchema.Designer.cs"
powershell.exe -command "(Get-Content Company.WebApplication1\Data\Migrations\ApplicationDbContextModelSnapshot.cs) -replace('[0-9].[0-9].[0-9]-[^-]*-[0-9]*', '%version%') | Set-Content Company.WebApplication1\Data\Migrations\ApplicationDbContextModelSnapshot.cs"
copy "Company.WebApplication1\Data\Migrations\*" "..\src\Microsoft.DotNet.Web.ProjectTemplates\content\StarterWeb-CSharp\Data\SqlLite"
rd "Company.WebApplication1" /s /q
