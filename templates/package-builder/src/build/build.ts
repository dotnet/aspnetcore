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
const yeomanGeneratorSource = './src/yeoman';

const dotNetPackages = {
    builtIn: 'Microsoft.DotNet.Web.Spa.ProjectTemplates',
    extra: 'Microsoft.AspNetCore.SpaTemplates'
};

interface TemplateConfig {
    dir: string;
    dotNetNewId: string;
    dotNetPackageId: string;
    displayName: string;
}

const templates: { [key: string]: TemplateConfig } = {
    'angular': { dotNetPackageId: dotNetPackages.builtIn, dir: '../../templates/AngularSpa/', dotNetNewId: 'Angular', displayName: 'Angular' },
    'aurelia': { dotNetPackageId: dotNetPackages.extra, dir: '../../templates/AureliaSpa/', dotNetNewId: 'Aurelia', displayName: 'Aurelia' },
    'knockout': { dotNetPackageId: dotNetPackages.extra, dir: '../../templates/KnockoutSpa/', dotNetNewId: 'Knockout', displayName: 'Knockout.js' },
    'react-redux': { dotNetPackageId: dotNetPackages.builtIn, dir: '../../templates/ReactReduxSpa/', dotNetNewId: 'ReactRedux', displayName: 'React.js and Redux' },
    'react': { dotNetPackageId: dotNetPackages.builtIn, dir: '../../templates/ReactSpa/', dotNetNewId: 'React', displayName: 'React.js' },
    'vue': { dotNetPackageId: dotNetPackages.extra, dir: '../../templates/VueSpa/', dotNetNewId: 'Vue', displayName: 'Vue.js' }
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

function leftPad(str: string, minLength: number, padChar: string) {
    while (str.length < minLength) {
        str = padChar + str;
    }
    return str;
}

function getBuildNumber(): string {
    if (process.env.APPVEYOR_BUILD_NUMBER) {
        return leftPad(process.env.APPVEYOR_BUILD_NUMBER, 6, '0');
    }

    // For local builds, use timestamp
    return 't-' + Math.floor((new Date().valueOf() - new Date(2017, 0, 1).valueOf()) / (60*1000));
}

function buildYeomanNpmPackage(outputRoot: string) {
    const outputTemplatesRoot = path.join(outputRoot, 'app/templates');
    rimraf.sync(outputTemplatesRoot);

    // Copy template files
    const filenameReplacements = [
        { from: /.*\.csproj$/, to: 'tokenreplace-namePascalCase.csproj' }
    ];
    const contentReplacements = [
        // Currently, there are none
    ];
    _.forEach(templates, (templateConfig, templateName) => {
        const outputDir = path.join(outputTemplatesRoot, templateName);
        writeTemplate(templateConfig.dir, outputDir, contentReplacements, filenameReplacements);
    });

    // Also copy the generator files (that's the compiled .js files, plus all other non-.ts files)
    const tempRoot = './tmp';
    copyRecursive(path.join(tempRoot, 'yeoman'), outputRoot, '**/*.js');
    copyRecursive(yeomanGeneratorSource, outputRoot, '**/!(*.ts)');

    // Clean up
    rimraf.sync(tempRoot);
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
                    defaultValue: 'netcoreapp2.0'
                },
                skipRestore: {
                    type: 'parameter',
                    datatype: 'bool',
                    description: 'If specified, skips the automatic restore of packages on project creation.',
                    defaultValue: 'false'
                }
            },
            tags: { language: 'C#', type: 'project' },
            postActions: [
                {
                    condition: '(!skipRestore)',
                    description: 'Restores NuGet packages required by this project.',
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
            ],
        }, null, 2));

        fs.writeFileSync(path.join(templateConfigDir, 'dotnetcli.host.json'), JSON.stringify({
            symbolInfo: {
                skipRestore: {
                    longName: 'no-restore',
                    shortName: ''
                }
            }
        }, null, 2));
        
        fs.writeFileSync(path.join(templateConfigDir, 'vs-2017.3.host.json'), JSON.stringify({
            name: { text: templateConfig.displayName },
            description: { text: `Web application built with MVC ASP.NET Core and ${templateConfig.displayName}` },
            order: 2000,
            icon: 'icon.png',
            learnMoreLink: 'https://github.com/aspnet/JavaScriptServices',
            uiFilters: [ 'oneaspnet' ]
        }, null, 2));
    });

    // Create the .nuspec file
    const yeomanPackageVersion = JSON.parse(fs.readFileSync(path.join(yeomanGeneratorSource, 'package.json'), 'utf8')).version;
    const nuspecContentTemplate = fs.readFileSync(`./src/dotnetnew/${ packageId }.nuspec`);
    writeFileEnsuringDirExists(outputRoot,
        `${ packageId }.nuspec`,
        applyContentReplacements(nuspecContentTemplate, [
            { from: /\{yeomanversion\}/g, to: yeomanPackageVersion },
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
const yeomanOutputRoot = path.join(distDir, 'generator-aspnetcore-spa');

rimraf.sync(distDir);
mkdirp.sync(artifactsDir);
buildYeomanNpmPackage(yeomanOutputRoot);
buildDotNetNewNuGetPackages(artifactsDir);

// Finally, create a .tar.gz file containing the built generator-aspnetcore-spa.
// The CI system can treat this as the final built artifact.
// Note that the targz APIs only come in async flavor.
targz().compress(yeomanOutputRoot, path.join(artifactsDir, 'generator-aspnetcore-spa.tar.gz'), err => {
    if (err) { throw err; }
});
