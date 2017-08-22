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
const webToolsVSPackageGuid = '{0CD94836-1526-4E85-87D3-FB5274C5AFC9}';

const dotNetPackages = {
    builtIn: 'Microsoft.DotNet.Web.Spa.ProjectTemplates',
    extra: 'Microsoft.AspNetCore.SpaTemplates'
};

interface TemplateConfig {
    dir: string;
    dotNetNewId: string;
    dotNetPackageId: string;
    displayName: string;
    localizationIdStart: number;
}

const templates: { [key: string]: TemplateConfig } = {
    'angular': { dotNetPackageId: dotNetPackages.builtIn, dir: '../../templates/AngularSpa/', dotNetNewId: 'Angular', displayName: 'Angular', localizationIdStart: 1100 },
    'aurelia': { dotNetPackageId: dotNetPackages.extra, dir: '../../templates/AureliaSpa/', dotNetNewId: 'Aurelia', displayName: 'Aurelia', localizationIdStart: 1200 },
    'knockout': { dotNetPackageId: dotNetPackages.extra, dir: '../../templates/KnockoutSpa/', dotNetNewId: 'Knockout', displayName: 'Knockout.js', localizationIdStart: 1300 },
    'react-redux': { dotNetPackageId: dotNetPackages.builtIn, dir: '../../templates/ReactReduxSpa/', dotNetNewId: 'ReactRedux', displayName: 'React.js and Redux', localizationIdStart: 1400 },
    'react': { dotNetPackageId: dotNetPackages.builtIn, dir: '../../templates/ReactSpa/', dotNetNewId: 'React', displayName: 'React.js', localizationIdStart: 1500 },
    'vue': { dotNetPackageId: dotNetPackages.extra, dir: '../../templates/VueSpa/', dotNetNewId: 'Vue', displayName: 'Vue.js', localizationIdStart: 1600 }
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

        // Add the .template.config dir and its contents
        const templateConfigDir = path.join(templateOutputDir, '.template.config');
        mkdirp.sync(templateConfigDir);

        fs.writeFileSync(path.join(templateConfigDir, 'template.json'), JSON.stringify({
            author: 'Microsoft',
            classifications: ['Web', 'MVC', 'SPA'],
            groupIdentity: `${packageId}.${templateConfig.dotNetNewId}`,
            identity: `${packageId}.${templateConfig.dotNetNewId}.CSharp`,
            name: `ASP.NET Core with ${templateConfig.displayName}`,
            preferNameDirectory: true,
            primaryOutputs: [{ path: `${sourceProjectName}.csproj` }],
            shortName: `${templateConfig.dotNetNewId.toLowerCase()}`,
            sourceName: sourceProjectName,
            sources: [{
                source: './',
                target: './',
                exclude: ['.template.config/**']
            }],
            symbols: {
                TargetFrameworkOverride: {
                    type: 'parameter',
                    description: 'Overrides the target framework',
                    replaces: 'TargetFrameworkOverride',
                    datatype: 'string',
                    defaultValue: ''
                },
                Framework: {
                    type: 'parameter',
                    description: 'The target framework for the project.',
                    datatype: 'choice',
                    choices: [
                        {
                            choice: 'netcoreapp2.0',
                            description: 'Target netcoreapp2.0'
                        }
                    ],
                    replaces: 'netcoreapp2.0',
                    defaultValue: 'netcoreapp2.0'
                },
                HostIdentifier: {
                    type: 'bind',
                    binding: 'HostIdentifier'
                },
                skipRestore: {
                    type: 'parameter',
                    datatype: 'bool',
                    description: 'If specified, skips the automatic restore of the project on create.',
                    defaultValue: 'false'
                }
            },
            tags: { language: 'C#', type: 'project' },
            postActions: [
                {
                    condition: '(!skipRestore)',
                    description: 'Restore NuGet packages required by this project.',
                    manualInstructions: [{ text: 'Run \'dotnet restore\'' }],
                    actionId: '210D431B-A78B-4D2F-B762-4ED3E3EA9025',
                    continueOnError: true
                },
                /*
                // Currently it doesn't appear to be possible to run `npm install` from a
                // postAction, due to https://github.com/dotnet/templating/issues/849
                // We will re-enable this when that bug is fixed.
                {
                    condition: '(!skipRestore)',
                    description: 'Restores NPM packages required by this project.',
                    manualInstructions: [{ text: 'Run \'npm install\'' }],
                    actionId: '3A7C4B45-1F5D-4A30-959A-51B88E82B5D2',
                    args: { executable: 'npm', args: 'install' },
                    continueOnError: false
                }
                */
                {
                    // For the preview2 release, just display manual instructions instead.
                    // This is only applicable on the command line, because VS will restore
                    // NPM packages automatically by default.
                    condition: '(HostIdentifier == "dotnetcli" || HostIdentifier == "dotnetcli-preview")',
                    actionId: 'AC1156F7-BB77-4DB8-B28F-24EEBCCA1E5C',
                    description: '\n\n-------------------------------------------------------------------\nIMPORTANT: Before running this project on the command line,\n           you must restore NPM packages by running "npm install"\n-------------------------------------------------------------------\n',
                    manualInstructions: [{ text: 'Run "npm install"' }]
                }
            ],
        }, null, 2));

        fs.writeFileSync(path.join(templateConfigDir, 'dotnetcli.host.json'), JSON.stringify({
            $schema: 'http://json.schemastore.org/dotnetcli.host',
            symbolInfo: {
                TargetFrameworkOverride: {
                    isHidden: 'true',
                    longName: 'target-framework-override',
                    shortName: ''
                },
                Framework: {
                    longName: 'framework'
                },
                skipRestore: {
                    longName: 'no-restore',
                    shortName: ''
                },
            }
        }, null, 2));
        
        const localisedNameId = templateConfig.localizationIdStart + 0;
        const localisedDescId = templateConfig.localizationIdStart + 1;

        fs.writeFileSync(path.join(templateConfigDir, 'vs-2017.3.host.json'), JSON.stringify({
            $schema: 'http://json.schemastore.org/vs-2017.3.host',
            name: { text: templateConfig.displayName, package: webToolsVSPackageGuid, id: localisedNameId.toString() },
            description: { text: `A project template for creating an ASP.NET Core application with ${templateConfig.displayName}`, package: webToolsVSPackageGuid, id: localisedDescId.toString() },
            order: 301,
            icon: 'icon.png',
            learnMoreLink: 'https://github.com/aspnet/JavaScriptServices',
            uiFilters: [ 'oneaspnet' ],
            minFullFrameworkVersion: '4.6.1'
        }, null, 2));
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

function runPrepublishScripts(rootDir: string, scripts: string[]) {
    console.log(`[Prepublish] In directory: ${ rootDir }`);
    scripts.forEach(script => {
        console.log(`[Prepublish] Running: ${ script }`);
        childProcess.execSync(script, { cwd: rootDir, stdio: 'inherit' });
    });
    console.log(`[Prepublish] Done`)
}

const distDir = './dist';
const artifactsDir = path.join(distDir, 'artifacts');

rimraf.sync(distDir);
mkdirp.sync(artifactsDir);
buildDotNetNewNuGetPackages(artifactsDir);
