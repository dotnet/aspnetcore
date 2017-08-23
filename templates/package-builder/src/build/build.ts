import * as glob from 'glob';
import * as gitignore from 'gitignore-parser';
import * as fs from 'fs';
import * as path from 'path';
import * as _ from 'lodash';
import * as mkdirp from 'mkdirp';
import * as rimraf from 'rimraf';
import * as childProcess from 'child_process';
import * as targz from 'tar.gz';

const isWindows = /^win/.test(process.platform);

const dotNetPackages = [
    'Microsoft.DotNet.Web.Spa.ProjectTemplates',
    'Microsoft.AspNetCore.SpaTemplates'
];

function getBuildNumber(): string {
    if (process.env.APPVEYOR_BUILD_NUMBER) {
        return process.env.APPVEYOR_BUILD_NUMBER;
    }

    // For local builds, use timestamp
    return Math.floor((new Date().valueOf() - new Date(2017, 0, 1).valueOf()) / (60*1000)) + '-local';
}

function buildDotNetNewNuGetPackages(outputDir: string) {
    const dotNetPackageIds = _.values(dotNetPackages);
    dotNetPackageIds.forEach(packageId => {
        const dotNetNewNupkgPath = buildDotNetNewNuGetPackage(packageId);

        // Move the .nupkg file to the output dir
        mkdirp.sync(outputDir);
        fs.renameSync(dotNetNewNupkgPath, path.join(outputDir, path.basename(dotNetNewNupkgPath)));
    });
}

function buildDotNetNewNuGetPackage(packageId: string) {
    // Invoke NuGet to create the final package
    const packageSourceRootDir = path.join('../', packageId);
    const nuspecFilename = `${ packageId }.nuspec`;
    const nugetExe = path.join(process.cwd(), './bin/NuGet.exe');
    const nugetStartInfo = { cwd: packageSourceRootDir, stdio: 'inherit' };
    const packageVersion = `1.0.${ getBuildNumber() }`;
    const nugetArgs = ['pack', nuspecFilename, '-Version', packageVersion];
    if (isWindows) {
        // Invoke NuGet.exe directly
        childProcess.spawnSync(nugetExe, nugetArgs, nugetStartInfo);
    } else {
        // Invoke via Mono (relying on that being available)
        nugetArgs.unshift(nugetExe);
        childProcess.spawnSync('mono', nugetArgs, nugetStartInfo);
    }

    return glob.sync(path.join(packageSourceRootDir, './*.nupkg'))[0];
}

buildDotNetNewNuGetPackages('./artifacts');
