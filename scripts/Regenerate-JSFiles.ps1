[cmdletbinding(SupportsShouldProcess = $true)]
param(
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2

Push-Location "src"
try {
    $dirs = Get-ChildItem -Directory
    foreach($dir in $dirs)
    {
        Push-Location $dir
        try{
            if(Test-Path -Path "package.json")
            {
                npm install
                npm run build
            }
        }
        finally{
            Pop-Location
        }
    }
}
finally {
    Pop-Location
}
