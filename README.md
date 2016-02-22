
How to run Coherence-Signed
===========================

1. Open a VS "Developer Command Prompt"

2. Depending on the Git branch you want to test (e.g. dev or release), run:
   ```
   set build_branch=dev
   set RootDrop=\\aspnetci\drops\Coherence\%build_branch%
   ```

3. Make a temporary edit to `dnx.msbuild` to disable BinScope and signing
   verification (you generally can't run them locally).
   Change line 14 from:
   ```
   <Target Name="Build" DependsOnTargets="CopyUnsignedPackages;BinScope;VerifySignatures" />
    ```
   To:
   ```
   <Target Name="Build" DependsOnTargets="CopyUnsignedPackages" />
    ```
   (Remember to undo this before you check in!)

4. Run the build file with TestCodeSign=true (because you can't do the
   real signing):
   ```
   msbuild dnx.msbuild /P:TestCodeSign=true
   ```

5. The output files will end up in `.\bin\Release\Packages`


Hints
=====

To change the source of packages, create a local path in the form:
```
    C:\FakeDNXDrop\Coherence\release\12345
```
Where `release` is the branch, and `12345` is the build number (the exact number doesn't matter).
Then in `.\tools\dnx.settings.targets` change `<DNXDropRoot>` to be `C:\FakeDNXDrop\`.

