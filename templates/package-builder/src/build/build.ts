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
const textFileExtensions = ['.gitignore', 'template_gitignore', '.config', '.cs', '.cshtml', '.csproj', '.html', '.js', '.json', '.jsx', '.md', '.nuspec', '.ts', '.tsx'];

const dotNetPackages = {
    builtIn: 'Microsoft.DotNet.Web.Spa.ProjectTemplates',
    extra: 'Microsoft.AspNetCore.SpaTemplates'
};

interface TemplateConfig {
    dir: string;
    dotNetPackageId: string;
}

const templates: { [key: string]: TemplateConfig } = {
    'angular': { dotNetPackageId: dotNetPackages.builtIn, dir: '../../templates/AngularSpa/' },
    'aurelia': { dotNetPackageId: dotNetPackages.extra, dir: '../../templates/AureliaSpa/' },
    'knockout': { dotNetPackageId: dotNetPackages.extra, dir: '../../templates/KnockoutSpa/' },
    'react-redux': { dotNetPackageId: dotNetPackages.builtIn, dir: '../../templates/ReactReduxSpa/' },
    'react': { dotNetPackageId: dotNetPackages.builtIn, dir: '../../templates/ReactSpa/' },
    'vue': { dotNetPackageId: dotNetPackages.extra, dir: '../../templates/VueSpa/' }
};

function isTextFile(filename: string): boolean {
    return textFileExtensions.indexOf(path.extname(filename).toLowerCase()) >= 0
        || textFileExtensions.indexOf(path.basename(filename)) >= 0;
}

function writeFileEnsuringDirExists(root: string, filename: string, contents: string | Buffer) {
    let fullPath = path.join(root, filename);
    mkdirp.sync(path.dirname(fullPath));
    fs.writeFileSync(fullPath, contents);
}

function listFilesExcludingGitignored(root: string): string[] {
    // Note that the gitignore files, prior to be written by the generator, are called 'template_gitignore'
    // instead of '.gitignore'. This is a workaround for Yeoman doing strange stuff with .gitignore files
    // (it renames them to .npmignore, which is not helpful).
    let gitIgnorePath = path.join(root, 'template_gitignore');
    let gitignoreEvaluator = fs.existsSync(gitIgnorePath)
        ? gitignore.compile(fs.readFileSync(gitIgnorePath, 'utf8'))
        : { accepts: () => true };
    return glob.sync('**/*', { cwd: root, dot: true, nodir: true })
        .filter(fn => gitignoreEvaluator.accepts(fn));
}

function applyContentReplacements(sourceContent: Buffer, contentReplacements: { from: RegExp, to: string }[]) {
    let sourceText = sourceContent.toString('utf8');
    contentReplacements.forEach(replacement => {
        sourceText = sourceText.replace(replacement.from, replacement.to);
    });

    return new Buffer(sourceText, 'utf8');
}

function writeTemplate(sourceRoot: string, destRoot: string, contentReplacements: { from: RegExp, to: string }[], filenameReplacements: { from: RegExp, to: string }[]) {
    listFilesExcludingGitignored(sourceRoot).forEach(fn => {
        let sourceContent = fs.readFileSync(path.join(sourceRoot, fn));
        if (isTextFile(fn)) {
            sourceContent = applyContentReplacements(sourceContent, contentReplacements);
        }

        // Also apply replacements in filenames
        filenameReplacements.forEach(replacement => {
            fn = fn.replace(replacement.from, replacement.to);
        });

        writeFileEnsuringDirExists(destRoot, fn, sourceContent);
    });
}

function copyRecursive(sourceRoot: string, destRoot: string, matchGlob: string) {
    glob.sync(matchGlob, { cwd: sourceRoot, dot: true, nodir: true })
        .forEach(fn => {
            const sourceContent = fs.readFileSync(path.join(sourceRoot, fn));
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
    const sourceProjectName = 'WebApplicationBasic';
    const projectGuid = '00000000-0000-0000-0000-000000000000';
    const filenameReplacements = [
        { from: /.*\.csproj$/, to: `${sourceProjectName}.csproj` },
        { from: /\btemplate_gitignore$/, to: '.gitignore' }
    ];
    const contentReplacements = [];
    _.forEach(templates, (templateConfig, templateName) => {
        // Only include templates matching the output package ID
        if (templateConfig.dotNetPackageId !== packageId) {
            return;
        }

        const templateOutputDir = path.join(outputRoot, 'Content', templateName);
        writeTemplate(templateConfig.dir, templateOutputDir, contentReplacements, filenameReplacements);
    });

    // Create the .nuspec file
    const nuspecContentTemplate = fs.readFileSync(`./src/dotnetnew/${ packageId }.nuspec`);
    writeFileEnsuringDirExists(outputRoot,
        `${ packageId }.nuspec`,
        applyContentReplacements(nuspecContentTemplate, [
            { from: /\{buildnumber\}/g, to: getBuildNumber() },
        ])
    );

    // Invoke NuGet to create the final package
    const nugetExe = path.join(process.cwd(), './bin/NuGet.exe');
    const nugetStartInfo = { cwd: outputRoot, stdio: 'inherit' };
    if (isWindows) {
        // Invoke NuGet.exe directly
        childProcess.spawnSync(nugetExe, ['pack'], nugetStartInfo);
    } else {
        // Invoke via Mono (relying on that being available)
        childProcess.spawnSync('mono', [nugetExe, 'pack'], nugetStartInfo);
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
