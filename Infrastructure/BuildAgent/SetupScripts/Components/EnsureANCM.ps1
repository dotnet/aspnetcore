. "$PSScriptRoot\_Common.ps1"

# Our Ensure-Msi script doesn't really support detecting separate MSIs per architectures well, so just always run the MSI. It should be idempotent
Ensure-Msi "ASP.NET Core Module 1.0 (x86)" $null "AspNetCoreModule\v1.0\aspnetcoremodule_x86_en.msi"
Ensure-Msi "ASP.NET Core Module 1.0 (x64)" $null "AspNetCoreModule\v1.0\aspnetcoremodule_x64_en.msi"
Ensure-Msi "ASP.NET Core Module for IIS Express 1.0 (x86)" $null "AspNetCoreModule\v1.0\ancm_iis_express_x86_en.msi"
Ensure-Msi "ASP.NET Core Module for IIS Express 1.0 (x64)" $null "AspNetCoreModule\v1.0\ancm_iis_express_x64_en.msi"