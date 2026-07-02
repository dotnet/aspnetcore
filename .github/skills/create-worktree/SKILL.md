---
name: create-worktree
description: >-
  Where and how to create an isolated git worktree in the dotnet/aspnetcore repo. USE FOR creating or adding a worktree, "make me a worktree", spinning up a scratch branch checkout, or setting up an isolated working copy of this repo. Covers what plain git worktree knowledge misses here: nest the worktree under .worktrees/<name> (which ships committed isolation so it stays out of git status and never inherits the top-level build configuration), copy those barrier files if you place the worktree elsewhere in the repo, initialize the submodules a fresh worktree leaves empty (MessagePack-CSharp, googletest), and share the main checkout's already-provisioned .dotnet SDK instead of re-downloading gigabytes. DO NOT USE FOR sibling worktrees outside the repo, general git branching without a worktree, or non-aspnetcore repositories.
---

# Create a worktree (dotnet/aspnetcore)

Use normal git, but get these repo-specific things right so the worktree is isolated and buildable. No script is needed; run the commands directly.

## 1. Create it under `.worktrees/<name>`

```bash
git worktree add -b <name> .worktrees/<name> HEAD
```

`.worktrees/` ships with committed isolation: a nested `.gitignore` that keeps every worktree out of the parent's `git status`, plus empty `Directory.Build.props`/`.targets`, an `.editorconfig` with `root = true`, and a `<clear/>` `NuGet.config`. These cap configuration traversal so the worktree stays out of git and never inherits the top-level build configuration. Do not recreate those files when you use `.worktrees/`.

If you instead place the worktree anywhere else inside the repo, copy those five files from `.worktrees/` into the directory that will contain the worktree, so it gets the same git-ignore and traversal-ceiling isolation. A sibling location outside the repo does not need them.

## 2. Initialize submodules in the new worktree

A fresh worktree has empty submodule working trees even though the main checkout is populated; builds that touch `src/submodules/MessagePack-CSharp` or `googletest` fail until you run:

```bash
git -C .worktrees/<name> submodule update --init --recursive
```

## 3. Share the `.dotnet` SDK

A fresh worktree has no `.dotnet`, so restoring would re-provision the whole SDK (gigabytes). When the worktree's `global.json` `sdk.version` matches the main checkout's already-provisioned SDK, link the parent's `.dotnet` into the worktree instead of restoring:

- Windows (no admin needed): `New-Item -ItemType Junction -Path <worktree>\.dotnet -Target <repo-root>\.dotnet`
- Linux / macOS: `ln -s "<repo-root>/.dotnet" "<worktree>/.dotnet"`

If the versions differ, run `./restore.cmd` (or `./restore.sh`) inside the worktree instead. Do not edit `global.json` to point `sdk.paths` at the parent SDK (it is tracked). When removing the worktree, delete the `.dotnet` link first (`cmd /c rmdir` on Windows, `rm` on Linux/macOS) so its target is not deleted through the link.
