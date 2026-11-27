param(
    [string] $BaseUrl = "http://localhost:5087",
    [switch] $VerifyFix
)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd("/")

function Invoke-ReproPost($Session, $Path)
{
    Invoke-RestMethod -Uri "$BaseUrl/repro/$Path" -Method Post -WebSession $Session
}

$control = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$start = Invoke-ReproPost $control "password"
if (-not $start.requiresTwoFactor)
{
    throw "Control setup failed: password sign-in did not require two factors."
}

$complete = Invoke-ReproPost $control "two-factor"
if (-not $complete.succeeded)
{
    throw "Control failed: two-factor sign-in should succeed without a passkey request."
}
Write-Host "CONTROL: password -> two-factor = Succeeded"

$collision = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$start = Invoke-ReproPost $collision "password"
$before = Invoke-RestMethod "$BaseUrl/repro/state" -WebSession $collision
if (-not $start.requiresTwoFactor -or $before.pendingUser -ne "repro@example.com")
{
    throw "Collision setup failed: the pending two-factor user was not established."
}

$null = Invoke-ReproPost $collision "passkey"
$after = Invoke-RestMethod "$BaseUrl/repro/state" -WebSession $collision
$complete = Invoke-ReproPost $collision "two-factor"

Write-Host "BEFORE passkey options: pending user = $($before.pendingUser)"
Write-Host "AFTER  passkey options: pending user = '$($after.pendingUser)'"
Write-Host "AFTER  passkey options: cookie property keys = $($after.propertyKeys -join ', ')"
Write-Host "COLLISION: password -> passkey options -> two-factor = $($complete.result)"

if ($VerifyFix)
{
    if ($after.pendingUser -ne "repro@example.com" -or -not $complete.succeeded)
    {
        throw "Fix verification failed: passkey options must preserve the pending user and allow two-factor sign-in."
    }
    Write-Host "VERIFIED: starting a passkey ceremony preserved the pending two-factor sign-in."
}
elseif ($after.pendingUser -or $complete.succeeded)
{
    throw "Issue 70207 was NOT reproduced: the expected two-factor state loss did not occur."
}
else
{
    Write-Host "REPRODUCED: starting a passkey ceremony destroyed the pending two-factor sign-in."
}
