# Backport Status for PR #65295

## Summary
This document describes the backport of PR #65295 (Update to Ubuntu 22.04) to release/9.0 and release/8.0 branches.

## PR #65295 Changes
The original PR updates Ubuntu from 20.04 to 22.04 in the Helix CI configuration files:
1. `eng/targets/Helix.Common.props`: Change `Ubuntu.2004.Amd64.Open` → `Ubuntu.2204.Amd64.Open`
2. `eng/targets/Helix.targets`: Change `Ubuntu.2004.Amd64.Open` → `Ubuntu.2204.Amd64.Open`
3. `eng/targets/Helix.targets`: Change `$(HelixQueueFedora40)` → `$(HelixQueueFedora)` (for release/10.0)

## Backport Branches Created

### 1. backport/65295-release-9.0
- **Status**: ✅ Complete (local branch created)
- **Base**: `origin/release/9.0`
- **Commit**: e5bb397da7
- **Changes**:
  - `eng/targets/Helix.Common.props`: Changed `Ubuntu.2004.Amd64.Open` → `Ubuntu.2204.Amd64.Open`
  - `eng/targets/Helix.targets`: Changed `Ubuntu.2004.Amd64.Open` → `Ubuntu.2204.Amd64.Open`
  - `eng/targets/Helix.targets`: Changed `$(HelixQueueFedora40)` → `$(HelixQueueFedora41)`

Note: For release/9.0, the Fedora queue variable was updated to `$(HelixQueueFedora41)` to match the Fedora version defined in that branch's `Helix.Common.props`.

### 2. backport/65295-release-8.0
- **Status**: ✅ Complete (local branch created)
- **Base**: `origin/release/8.0`
- **Commit**: f5cc31e943
- **Changes**:
  - `eng/targets/Helix.Common.props`: Changed `Ubuntu.2004.Amd64.Open` → `Ubuntu.2204.Amd64.Open`
  - `eng/targets/Helix.targets`: Changed `Ubuntu.2004.Amd64.Open` → `Ubuntu.2204.Amd64.Open`
  - `eng/targets/Helix.targets`: Changed `$(HelixQueueFedora40)` → `$(HelixQueueFedora41)`

Note: For release/8.0, the Fedora queue variable was updated to `$(HelixQueueFedora41)` to match the Fedora version defined in that branch's `Helix.Common.props`.

## Patch Files

For convenience, patch files have been generated in the `backport-patches/` directory:
- `backport-patches/release-9.0.patch` - Patch for release/9.0
- `backport-patches/release-8.0.patch` - Patch for release/8.0

These can be applied using:
```bash
# Apply to release/9.0
git checkout release/9.0
git apply backport-patches/release-9.0.patch

# Apply to release/8.0
git checkout release/8.0
git apply backport-patches/release-8.0.patch
```

## Next Steps

Due to environment authentication limitations, the backport branches have been created and committed locally but not pushed to the remote repository. To complete the backport:

### Option 1: Push existing local branches
If the local branches still exist:
```bash
# Push the backport branches
git push origin backport/65295-release-9.0
git push origin backport/65295-release-8.0
```

### Option 2: Apply patches and create branches
If the local branches are not available:
```bash
# For release/9.0
git checkout -b backport/65295-release-9.0 origin/release/9.0
git apply backport-patches/release-9.0.patch
git add .
git commit -m "Backport PR #65295 to release/9.0: Update to Ubuntu 22.04"
git push origin backport/65295-release-9.0

# For release/8.0
git checkout -b backport/65295-release-8.0 origin/release/8.0
git apply backport-patches/release-8.0.patch
git add .
git commit -m "Backport PR #65295 to release/8.0: Update to Ubuntu 22.04"
git push origin backport/65295-release-8.0
```

### Option 3: Create PRs via GitHub CLI
```bash
# Create PR for release/9.0
gh pr create --base release/9.0 --head backport/65295-release-9.0 \
  --title "[release/9.0] Update to Ubuntu 22.04" \
  --body "Backport of #65295 to release/9.0

This PR updates the Ubuntu version from 20.04 to 22.04 in Helix CI configuration files.

Changes:
- Update Ubuntu version in Helix.Common.props
- Update Ubuntu version in Helix.targets
- Update Fedora queue reference to match branch configuration

Original PR: https://github.com/dotnet/aspnetcore/pull/65295"

# Create PR for release/8.0
gh pr create --base release/8.0 --head backport/65295-release-8.0 \
  --title "[release/8.0] Update to Ubuntu 22.04" \
  --body "Backport of #65295 to release/8.0

This PR updates the Ubuntu version from 20.04 to 22.04 in Helix CI configuration files.

Changes:
- Update Ubuntu version in Helix.Common.props
- Update Ubuntu version in Helix.targets
- Update Fedora queue reference to match branch configuration

Original PR: https://github.com/dotnet/aspnetcore/pull/65295"
```

### Option 4: Create PRs via GitHub Web UI
1. Push both branches to origin
2. Navigate to https://github.com/dotnet/aspnetcore/pulls
3. Click "New pull request"
4. Select base branch: `release/9.0`, compare branch: `backport/65295-release-9.0`
5. Fill in PR title and description
6. Repeat for release/8.0

## Verification

The changes in both backport branches are minimal and targeted:
- Only 2 files modified in each branch
- 3 lines changed in each branch (3 insertions, 3 deletions)
- Changes match the intent of the original PR #65295
- Fedora queue variable updated appropriately for each release branch

## Files Changed

### eng/targets/Helix.Common.props
```diff
-        <HelixAvailableTargetQueue Include="Ubuntu.2004.Amd64.Open" Platform="Linux" />
+        <HelixAvailableTargetQueue Include="Ubuntu.2204.Amd64.Open" Platform="Linux" />
```

### eng/targets/Helix.targets
```diff
-      $(HelixQueueFedora40);
-      Ubuntu.2004.Amd64.Open;
+      $(HelixQueueFedora41);
+      Ubuntu.2204.Amd64.Open;
```
