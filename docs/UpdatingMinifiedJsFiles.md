# Updating minified .js files

Building our `src/Components` projects will produce minified.js files under `src/Components/Web.JS/dist/Release`. In order to avoid constant merge conflicts, and to avoid having to restore NPM components over the network during offline source-build, we keep the latest versions of those files in a submodule repo, https://github.com/dotnet/blazorminifiedjs. If you are prepping a PR that is going to change the contents of those files, please follow the steps in this doc.

1. Build the node components of the repo
    1. Running `npm run build` from the repo root should be sufficient, assuming you have already installed the prereqs listed in our [Building from source doc](https://github.com/dotnet/aspnetcore/edit/main/docs/BuildFromSource.md).
2. In a separate folder, clone the [BlazorMinifiedJs repo](https://github.com/dotnet/blazorminifiedjs).
3. Check out a new branch in your clone, based off of `main`.
4. Replace the files in `BlazorMinifiedJs/src` with the minified .js files you just generated in aspnetcore (these can be found at `aspnetcore/src/Components/Web.JS/dist/Release`).
5. Push your `BlazorMinifiedJs` branch and open a PR in that repo.
6. Once that PR has been merged, return to your aspnetcore PR, navigate to `src/submodules/BlazorMinifiedJs`, and checkout the commit you just pushed.
7. Push the submodule update to your aspnetcore PR.

Alternatively, you can find the generated .js files in the artifacts of your PR build, under the artifact named "Minified_JS_Files". This may be more reliable than building the node components locally.

Following these steps should remediate any build errors related to `BlazorMinifiedJs` in your PR.