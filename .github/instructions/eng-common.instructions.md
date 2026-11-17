---
description: Instructions for Arcade-owned files under eng/common
applyTo: "eng/common/**"
---

Files under `eng/common` come from
[dotnet/arcade](https://github.com/dotnet/arcade) and are synchronized into this
repository. Read `eng/common/AGENTS.md` and `eng/common/README.md` for the
authoritative ownership guidance.

When reviewing a change authored directly in this repository, report that the
local edit will be overwritten and that the durable change must be made in
Arcade and flowed back to ASP.NET Core.

Do not report this for upstream dependency, source, mirror, or inter-branch
flows, or when the pull request provenance is unclear. Keep the finding about
ownership and durability; do not invent a defect in the changed code merely
because the file is under `eng/common/**`.
