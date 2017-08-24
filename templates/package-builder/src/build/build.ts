import * as fs from 'fs';
import * as path from 'path';
import * as mkdirp from 'mkdirp';
import * as childProcess from 'child_process';

const isWindows = /^win/.test(process.platform);
const packageIds = [
    'Microsoft.DotNet.Web.Spa.ProjectTemplates',
    'Microsoft.AspNetCore.SpaTemplates'
];
const packageVersion = `1.0.${ getBuildNumber() }`;

function getBuildNumber(): string {
    if (process.env.APPVEYOR_BUILD_NUMBER) {
        return process.env.APPVEYOR_BUILD_NUMBER;
    }

    // For local builds, use timestamp
    return Math.floor((new Date().valueOf() - new Date(2017, 0, 1).valueOf()) / (60*1000)) + '-local';
}

packageIds.forEach(packageId => {
    // Invoke NuGet to create the final package
    const packageSourceRootDir = path.join('../', packageId);
    const nuspecFilename = `${ packageId }.nuspec`;
    const nugetExe = path.resolve('./bin/NuGet.exe');
    const nugetStartInfo = { cwd: packageSourceRootDir, stdio: 'inherit' };
    const nugetArgs = [
        'pack', nuspecFilename,
        '-Version', packageVersion,
        '-OutputDirectory', path.resolve('./artifacts')
    ];

    if (isWindows) {
        // Invoke NuGet.exe directly
        childProcess.spawnSync(nugetExe, nugetArgs, nugetStartInfo);
    } else {
        // Invoke via Mono (relying on that being available)
        nugetArgs.unshift(nugetExe);
        childProcess.spawnSync('mono', nugetArgs, nugetStartInfo);
    }
});
