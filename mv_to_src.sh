#!/bin/bash

dir="$(pwd)"
name="IISIntegration"

if [ "$skip_src" != false ]; then
    echo "Moving $dir into src/$name/"
    if [ -d "src/" ]; then
        git mv src/ src_tmp/
    fi
    mkdir -p "src/$name"
    if [ -d "src_tmp/" ]; then
        git mv src_tmp/ "src/$name/src/"
    fi
fi

files_to_mv=(NuGetPackageVerifier.json .gitignore README.md version.props Directory.Build.props Directory.Build.targets *.sln shared test tools samples build NuGet benchmarks korebuild.json nuget)
for f in "${files_to_mv[@]}"; do
    if [ -e $f ]; then
        echo "Moving $f"
        git mv $f "src/$name/$f"
    fi
done

files_to_rm=(build.sh build.cmd run.cmd run.sh run.ps1 NuGet.config korebuild-lock.txt .github .vsts-pipelines .vscode .appveyor.yml .travis.yml CONTRIBUTING.md)
for f in "${files_to_rm[@]}"; do
    if [ -e $f ]; then
        echo "Removing $f"
        git rm -r $f
    fi
done

echo "Reorganize source code from aspnet/$name into a subfolder" > .git/COMMIT_EDITMSG
echo "" >> .git/COMMIT_EDITMSG
echo "Prior to reorg, this source existed at https://github.com/aspnet/$name/tree/$(git rev-parse HEAD)" >> .git/COMMIT_EDITMSG