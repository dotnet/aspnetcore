# Passkey / two-factor cookie collision reproduction

Reproduces [dotnet/aspnetcore#70207](https://github.com/dotnet/aspnetcore/issues/70207)
using the real `SignInManager`, cookie authentication, and this sample's existing
in-memory SQLite database. No physical passkey, authenticator, email service,
registration, or persistent database is required.

## Run on Windows

From the repository root in PowerShell, activate the repository SDK:
The sample has already been built in this checkout, so `--no-build` avoids
rebuilding the in-tree framework dependencies.

```powershell
Set-Location D:\PassKeyIssue
. .\activate.ps1
dotnet run --project .\src\Identity\samples\IdentitySample.ApiEndpoints --no-build --no-launch-profile -- --urls http://localhost:5087 --environment Development
```

Wait for `Now listening on: http://localhost:5087`.
In a second PowerShell terminal, run:

```powershell
Set-Location D:\PassKeyIssue
.\src\Identity\samples\IdentitySample.ApiEndpoints\Reproduce70207.ps1
```

If running on another port, pass `-BaseUrl http://localhost:YOUR_PORT`.
Stop the server with Ctrl+C when finished.

This is an in-repository sample using the repository's current target framework
and in-tree Identity project references, not a standalone .NET 10 application.
An unprepared checkout needs the normal repository prerequisites and restore
described in [BuildFromSource](../../../../docs/BuildFromSource.md).
On a fresh checkout, omit `--no-build` for the first run. The first build can
take substantially longer because it also compiles framework dependencies.

## What happens

The diagnostic endpoints are available only in the Development environment.
Startup creates the disposable user `repro@example.com`, password `Repro-70207!`,
with confirmed email and two-factor authentication enabled.

The script uses two independent cookie jars and separate HTTP requests:

1. **Control:** `/repro/password` calls `PasswordSignInAsync`, which returns
   `TwoFactorRequired`. `/repro/two-factor` generates a genuine email-provider
   token and completes `TwoFactorSignInAsync` successfully.
2. **Collision:** begin the same password sign-in and confirm
   `GetTwoFactorAuthenticationUserAsync` returns the pending user.
   Call `/repro/passkey`, which uses `MakePasskeyRequestOptionsAsync(user: null)`.
   Check the pending user again, then submit another valid two-factor token.

Expected output on an affected build:

```text
CONTROL: password -> two-factor = Succeeded
BEFORE passkey options: pending user = repro@example.com
AFTER  passkey options: pending user = ''
AFTER  passkey options: cookie property keys = ...
COLLISION: password -> passkey options -> two-factor = Failed
REPRODUCED: starting a passkey ceremony destroyed the pending two-factor sign-in.
```

The script stops with an error if the control or collision prerequisites fail,
or if the expected loss of two-factor state is not observed.
An exit code of zero means the reported bug **was reproduced**, not fixed.

The failure occurs when requesting passkey options, before an authenticator is
invoked. This reproduction deliberately stops there: it does not simulate a
WebAuthn credential, prove a complete browser passkey flow, or exercise the
reverse ordering / passkey-creation variant. The sample uses the framework's
own state persistence without rewriting authentication cookies.

## Validation

Verified with this checkout's .NET 12 preview SDK and in-tree Identity:

- `dotnet build .\src\Identity\samples\IdentitySample.ApiEndpoints\IdentitySample.ApiEndpoints.csproj --no-restore -v minimal`
  succeeded with zero warnings and zero errors.
- The server command above started successfully.
- `.\src\Identity\samples\IdentitySample.ApiEndpoints\Reproduce70207.ps1`
  exited with code zero: the control succeeded, the pending user became null
  after requesting passkey options, and the subsequent two-factor sign-in failed.

This confirms the HTTP/cookie-level reproduction, not a bug fix or browser E2E test.

## Validate the fix

After rebuilding the sample with the framework fix, run the same server command
and use:

```powershell
.\src\Identity\samples\IdentitySample.ApiEndpoints\Reproduce70207.ps1 -VerifyFix
```

In this mode both sign-ins must succeed, and the pending user after requesting
passkey options must still be `repro@example.com`. The original invocation
without `-VerifyFix` continues to expect the bug.

The fix uses a separate `Identity.Passkey` cookie for passkey ceremony state.
`AddIdentity`, `AddDefaultIdentity`, and `AddIdentityCookies` register it
automatically. Applications that register the individual Identity cookies
themselves can use the existing `AddCookie("Identity.Passkey", ...)` API.
Customize it with the existing named-options API, rather than the two-factor
cookie options:

```csharp
services.Configure<CookieAuthenticationOptions>("Identity.Passkey", options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
});
```

The scheme identifier and registration helper are internal; no public types,
members, or API baseline entries are added by the fix.
An in-flight passkey ceremony started on the old build must be restarted
after upgrading; old two-factor cookies retain their existing scheme.

Keeping both flows in one cookie would require merging state and selectively
clearing it on every path. A separate cookie keeps their replacement,
expiration, and consumption independent.

### Fix verification results

- `PasskeyCeremonyAndTwoFactorSignInHaveIndependentState` failed on all eight
  rows before the fix and passed on all eight after it. The rows cover both
  start orderings, assertion and attestation, and `AddIdentity` versus
  `AddIdentityCore` plus `AddIdentityCookies`. The test uses real Identity and
  cookie middleware; only the passkey handler is mocked. It checks pending
  user preservation, consumption of the original ceremony state, and successful
  two-factor sign-in after the passkey validation attempt.
- The full cookie functional test class passed: 30 passed, 0 failed, 0 skipped.
- The affected sign-in manager and options tests passed: 125 passed, 0 failed,
  0 skipped, including passkey cookie customization and sign-out cleanup.
- Both test scopes were rerun successfully after internalizing the cookie
  implementation; the public API baselines remain unchanged.
- During initial fix validation, the sample rebuilt with zero warnings and zero errors, and
  `Reproduce70207.ps1 -VerifyFix` exited with code zero.

Commands used from the activated repository root:

```powershell
dotnet test .\src\Identity\test\InMemory.Test\Microsoft.AspNetCore.Identity.InMemory.Test.csproj --no-restore --filter 'FullyQualifiedName~PasskeyCeremony' --logger 'console;verbosity=normal' --logger 'trx;LogFileName=70207-green.trx'
dotnet build .\src\Identity\Core\src\Microsoft.AspNetCore.Identity.csproj --no-restore -p:BuildProjectReferences=false -v minimal
dotnet test .\src\Identity\test\InMemory.Test\Microsoft.AspNetCore.Identity.InMemory.Test.csproj --no-restore -p:BuildProjectReferences=false --filter 'FullyQualifiedName~Microsoft.AspNetCore.Identity.InMemory.FunctionalTest' --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=70207-internal-functional.trx'
dotnet test .\src\Identity\test\Identity.Test\Microsoft.AspNetCore.Identity.Test.csproj --no-restore -p:BuildProjectReferences=false --filter 'FullyQualifiedName~SignInManagerTest|FullyQualifiedName~IdentityOptionsTest' --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=70207-internal-identity.trx'
```

The public API baselines remain unchanged. Complete browser/WebAuthn validation
was not run.
The final Identity build completed with zero warnings and zero errors.
`BuildProjectReferences=false` reuses dependencies already built during the
initial validation; omit it when those dependency outputs are unavailable.
