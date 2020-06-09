$contents = Get-Content Company.WebApplication1\Company.WebApplication1.csproj -Raw
$matches = [Regex]::Match($contents, 'Version=\"(?<version>[^\"]+)\"');
$appVer = $matches.Groups[1].Value
$replace = 'App" Version="' + $appVer + '"';
$contents -replace('App\"', $replace) | Set-Content Company.WebApplication1\Company.WebApplication1.csproj
