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

function writeFileEnsuringDirExists(root: string, filename: string, contents: string | Buffer) {
    let fullPath = path.join(root, filename);
    mkdirp.sync(path.dirname(fullPath));
    fs.writeFileSync(fullPath, contents);
}

function listFilesExcludingGitignored(root: string): string[] {
    let gitIgnorePath = path.join(root, '.gitignore');
    let gitignoreEvaluator = fs.existsSync(gitIgnorePath)
        ? gitignore.compile(fs.readFileSync(gitIgnorePath, 'utf8'))
        : { accepts: () => true };
    return glob.sync('**/*', { cwd: root, dot: true, nodir: true })
        .filter(fn => gitignoreEvaluator.accepts(fn));
}

function writeTemplate(sourceRoot: string, destRoot: string) {
    listFilesExcludingGitignored(sourceRoot).forEach(fn => {
        let sourceContent = fs.readFileSync(path.join(sourceRoot, fn));
        writeFileEnsuringDirExists(destRoot, fn, sourceContent);
    });
}

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
        fs.renameSync(dotNetNewNupkgPath, path.join(outputDir, path.basename(dotNetNewNupkgPath)));
    });
}

function buildDotNetNewNuGetPackage(packageId: string) {
    const outputRoot = './dist/dotnetnew';
    rimraf.sync(outputRoot);

    // Copy template files
    const packageSourceRootDir = path.join('../', packageId);
    const templatesInPackage = fs.readdirSync(path.join(packageSourceRootDir, 'Content'));

    _.forEach(templatesInPackage, templateName => {
        const templateSourceDir = path.join(packageSourceRootDir, 'Content', templateName);
        const templateOutputDir = path.join(outputRoot, 'Content', templateName);
        writeTemplate(templateSourceDir, templateOutputDir);
    });

    // Create the .nuspec file
    const nuspecFilename = `${ packageId }.nuspec`;
    const nuspecContentTemplate = fs.readFileSync(path.join(packageSourceRootDir, nuspecFilename));
    writeFileEnsuringDirExists(outputRoot,
        nuspecFilename,
        nuspecContentTemplate
    );

    // Invoke NuGet to create the final package
    const nugetExe = path.join(process.cwd(), './bin/NuGet.exe');
    const nugetStartInfo = { cwd: outputRoot, stdio: 'inherit' };
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

    // Clean up
    rimraf.sync('./tmp');

    return glob.sync(path.join(outputRoot, './*.nupkg'))[0];
}

const distDir = './dist';
const artifactsDir = path.join(distDir, 'artifacts');

rimraf.sync(distDir);
mkdirp.sync(artifactsDir);
buildDotNetNewNuGetPackages(artifactsDir);
