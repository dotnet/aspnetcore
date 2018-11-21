# Upgrading Mono

## Obtaining a Mono build

1. Find the latest Mono WebAssembly builds at https://jenkins.mono-project.com/job/test-mono-mainline-wasm/

1. Pick the build you want from the *Build History* pane (e.g., most recent green build).

1. At the bottom of the build info page, navigate to the chosen configuration (currently there's only one, called *Default*).

1. Now on the sidebar, navigate to *Azure Artifacts*.

1. Download the .zip file. Note that the commit's SHA hash is in the filename - you'll need that later to track which Mono version we're using in Blazor. 

**Shortcut:** Browse directly to https://jenkins.mono-project.com/job/test-mono-mainline-wasm/255/label=ubuntu-1804-amd64/Azure/, replacing the number 255 with the desired build number.

## Updating Blazor's `src\mono\incoming` directory

1. Extract the contents of the Mono build .zip file to a temporary directory.

1. Replace the contents of `Blazor\src\mono\incoming` with the equivalents from the new Mono build:

   * In Blazor's `src\mono\incoming\wasm` dir, replace `mono.wasm` and `mono.js` with the new files from Mono's `release` dir
   * In Blazor's `src\mono\incoming\bcl`, delete all the `.dll` files (including from the `Facades` subdirectory), and copy in all the new `.dll` files from Mono's `wasm-bcl\wasm` dir. **Note:** We *only* need the `.dll` files, so don't include `.pdb`/`.cs`/`.tmp`/`.stamp` or others. Also you can omit `nunitlite.dll` - we don't need that either.

The net effect is that you're replacing everything with the newer versions, including adding any new `.dll` files and removing any older `.dll` files that are no longer involved.

**Commit**

At this stage, make a Git commit with a message similar to `Upgrade Mono binaries to <their-commit-sha>`. Their commit SHA can be found in the filename of the Mono drop you downloaded.

## Verifying

Rebuild Blazor completely from a clean state:

 * `cd` to the root of your Blazor repo
 * Verify `git status` shows your working copy has no uncommitted edits
 * `git clean -xdf`
 * `git submodule foreach git clean -xdf`
 * `build.cmd` or `./build.sh`

Now run the E2E tests.

If anything seems broken, you might want to start investigations in the MonoSanity project, since this just uses Mono directly, without most of what Blazor builds on top of it.
