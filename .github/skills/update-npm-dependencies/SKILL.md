---
name: update-npm-dependencies
description: >-
  Refresh npm dependencies or fix npm Component Governance and Dependabot alerts in
  dotnet/aspnetcore. Regenerate lockfiles, ingest resolved versions into dotnet-public-npm,
  and validate the update on main or a release branch. Also use for ingestion-only requests.
---

# Update npm dependencies in ASP.NET Core

Use the requested `dotnet/aspnetcore` branch, defaulting to `main`. Identify the upstream
remote and an authorized push remote from `git remote -v`; do not assume either remote
name or fork owner. Start from a clean worktree based on the requested upstream branch.
For an existing PR, start from its current head and add a commit without amending,
force-pushing, or opening a duplicate.

Before the user acts on a `dotnet/aspnetcore` PR targeting `release/*`, remind them to
[temporarily elevate to administrator](https://repos.opensource.microsoft.com/orgs/dotnet/repos/aspnetcore).

## 1. Find the owning lockfiles

Run `git ls-files '*package.json' '*package-lock.json' '*npm-shrinkwrap.json' '*yarn.lock'`.
Inspect root workspaces and independent projects separately. For a full refresh, include
`eng/skill-evals/evaluation-tools` if present (it is not a root workspace), and inspect
other independent projects such as `src/Mvc/build` and `src/ProjectTemplates/Web.ProjectTemplates`.
For a targeted fix, use the alert's affected range and dependency paths to find each owning
lockfile and nested copy. Do not create a lockfile for a project that does not use it or
change unrelated pins and overrides.

## 2. Configure the Azure Artifacts npm feed

Use the branch's supported Node and package-manager versions and follow
`eng/scripts/update-npm-dependencies.ps1` where applicable. Use an account authorized to save upstream packages in `dotnet-public-npm`.
If interactive `vsts-npm-auth` is not available, use Azure CLI authentication instead.
Sign in first (`az login --use-device-code` if needed). Use a private temporary config,
never change checked-in `.npmrc` files, and don't echo or log the token.

```bash
(
  set -eu
  umask 077
  auth_dir=$(mktemp -d)
  trap 'rm -f "$auth_dir/npmrc"; rmdir "$auth_dir"' EXIT
  export NPM_CONFIG_USERCONFIG="$auth_dir/npmrc"
  feed=pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public-npm/npm/registry/
  token=$(az account get-access-token --resource 499b84ac-1321-427f-aa17-267ca6975798 \
    --query accessToken -o tsv)
  password=$(printf '%s' "$token" | base64 | tr -d '\n')
  printf 'registry=https://%s\n//%s:username=AzureDevOps\n//%s:_password=%s\n//%s:email=unused@example.invalid\n' \
    "$feed" "$feed" "$feed" "$password" "$feed" > "$NPM_CONFIG_USERCONFIG"
  unset token password
  npm whoami --registry="https://$feed"
  # Run the selected update and ingestion commands here before the config is removed.
)
```

## 3. Update and ingest

- For a targeted security fix, keep the lockfiles and use package-manager update commands.
  Prefer a compatible update to the package or its direct parent; use an override only when
  needed and verify every nested copy. Never use `npm audit fix --force` for routine updates.
- For a full npm refresh, do what the repo script does in a clean worktree with no installed
  dependencies: remove `node_modules` and `package-lock.json`, then run
  `npm install --prefer-online --include optional` in the root and each selected independent npm
  project. Existing lockfiles or installed packages make npm keep their resolutions. Regenerate with the owning package manager; do not
  hand-edit lockfiles or run blanket cleanup in a shared checkout.
- Check the seven-day feed quarantine using the exact version's registry publish time.
  Prefer a compatible patched version already eligible for ingestion. If none is eligible,
  report the blocker and when the candidate becomes eligible; do not bypass the feed or
  claim the alert is fixed.
- Ingest concrete registry `name@version` pairs from every updated lockfile with `npm cache add`
  against the authenticated feed. Use a fresh cache for ingestion so an existing cache hit
  doesn't mask a missing feed version. Deduplicate pairs and exclude bundled packages,
  workspace links, and other local dependencies. Include optional platform packages the local
  install skipped, including Rollup platform packages and the matching `@rollup/wasm-node`
  where the repository uses Rollup. Check each result.
- For ingestion-only requests, resolve the requested ranges to concrete versions and cache
  those versions without editing or opening a PR. If asked to ingest from `yarn.lock`,
  read the files on the requested branch, use each resolved version (not the requested range),
  ignore `link:`/`file:` and local paths, and deduplicate across the files.

## 4. Validate and publish

After the temporary auth config is removed, verify clean installs against the feed without
credentials or a warm cache to match CI. Run the affected builds/tests, including any
independent project scripts. Install the skill-evaluation harness in a scratch directory
when validating skill layout so its `node_modules` do not affect fixture discovery.

For security updates, check the resulting dependency graph against the advisory and the
requested Component Governance and Dependabot alerts. `npm audit` alone is not enough.
Distinguish fixed dependencies in the proposed change from live alerts, which stay open
until merge and rescanning. Report any unresolved versions, missing upstream fix,
quarantine, or policy review. Review `git diff --check` and ensure only intended dependency
files changed and no credentials were staged.

When asked to publish, commit the intended files and push to the authorized remote. Open
the PR against the selected branch of `dotnet/aspnetcore`, or update the existing PR if
requested. Describe the affected projects, fixes, validation, and blockers. Use upstream
PR and issue links, not fork links.
