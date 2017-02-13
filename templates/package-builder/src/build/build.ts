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
const textFileExtensions = ['.gitignore', 'template_gitignore', '.config', '.cs', '.cshtml', '.csproj', 'Dockerfile', '.html', '.js', '.json', '.jsx', '.md', '.nuspec', '.ts', '.tsx', '.xproj'];
const yeomanGeneratorSource = './src/yeoman';

// To support the "dotnet new" templates, we want to bundle prebuilt dist dev-mode files, because "dotnet new" can't auto-run
// webpack on project creation. Note that these script entries are *not* the same as the project's usual prepublish
// scripts, because here we want dev-mode builds (e.g., to support HMR), not prod-mode builds.
const commonTemplatePrepublishSteps = [
    'npm install',
    'node node_modules/webpack/bin/webpack.js --config webpack.config.vendor.js',
    'node node_modules/webpack/bin/webpack.js'
];
const commonForceInclusionRegex = /^(wwwroot|ClientApp)\/dist\//; // Files to be included in template, even though gitignored

const templates: { [key: string]: { dir: string, dotNetNewId: string, displayName: string, prepublish?: string[], forceInclusion?: RegExp } } = {
    'angular': { dir: '../../templates/Angular2Spa/', dotNetNewId: 'Angular', displayName: 'Angular' },
    'aurelia': { dir: '../../templates/AureliaSpa/', dotNetNewId: 'Aurelia', displayName: 'Aurelia' },
    'knockout': { dir: '../../templates/KnockoutSpa/', dotNetNewId: 'Knockout', displayName: 'Knockout.js' },
    'react-redux': { dir: '../../templates/ReactReduxSpa/', dotNetNewId: 'ReactRedux', displayName: 'React.js and Redux' },
    'react': { dir: '../../templates/ReactSpa/', dotNetNewId: 'React', displayName: 'React.js' }
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

function listFilesExcludingGitignored(root: string, forceInclusion: RegExp): string[] {
    // Note that the gitignore files, prior to be written by the generator, are called 'template_gitignore'
    // instead of '.gitignore'. This is a workaround for Yeoman doing strange stuff with .gitignore files
    // (it renames them to .npmignore, which is not helpful).
    let gitIgnorePath = path.join(root, 'template_gitignore');
    let gitignoreEvaluator = fs.existsSync(gitIgnorePath)
        ? gitignore.compile(fs.readFileSync(gitIgnorePath, 'utf8'))
        : { accepts: () => true };
    return glob.sync('**/*', { cwd: root, dot: true, nodir: true })
        .filter(fn => gitignoreEvaluator.accepts(fn) || (forceInclusion && forceInclusion.test(fn)));
}

function writeTemplate(sourceRoot: string, destRoot: string, contentReplacements: { from: RegExp, to: string }[], filenameReplacements: { from: RegExp, to: string }[], forceInclusion: RegExp) {
    listFilesExcludingGitignored(sourceRoot, forceInclusion).forEach(fn => {
        let sourceContent = fs.readFileSync(path.join(sourceRoot, fn));

        // For text files, replace hardcoded values with template tags
        if (isTextFile(fn)) {
            let sourceText = sourceContent.toString('utf8');
            contentReplacements.forEach(replacement => {
                sourceText = sourceText.replace(replacement.from, replacement.to);
            });

            sourceContent = new Buffer(sourceText, 'utf8');
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

function buildYeomanNpmPackage(outputRoot: string) {
    const outputTemplatesRoot = path.join(outputRoot, 'app/templates');
    rimraf.sync(outputTemplatesRoot);

    // Copy template files
    const filenameReplacements = [
        { from: /.*\.xproj$/, to: 'tokenreplace-namePascalCase.xproj' },
        { from: /.*\.csproj$/, to: 'tokenreplace-namePascalCase.csproj' }
    ];
    const contentReplacements = [
        // Dockerfile items
        { from: /FROM microsoft\/dotnet:1.1.0-sdk-projectjson/g, to: 'FROM <%= dockerBaseImage %>' },

        // .xproj items
        { from: /\bWebApplicationBasic\b/g, to: '<%= namePascalCase %>' },
        { from: /<ProjectGuid>[0-9a-f\-]{36}<\/ProjectGuid>/g, to: '<ProjectGuid><%= projectGuid %></ProjectGuid>' },
        { from: /<RootNamespace>.*?<\/RootNamespace>/g, to: '<RootNamespace><%= namePascalCase %></RootNamespace>'},
        { from: /\s*<BaseIntermediateOutputPath.*?<\/BaseIntermediateOutputPath>/g, to: '' },
        { from: /\s*<OutputPath.*?<\/OutputPath>/g, to: '' },

        // global.json items
        { from: /1\.0\.0-preview2-1-003177/, to: '<%= sdkVersion %>' }
    ];
    _.forEach(templates, (templateConfig, templateName) => {
        const outputDir = path.join(outputTemplatesRoot, templateName);
        writeTemplate(templateConfig.dir, outputDir, contentReplacements, filenameReplacements, commonForceInclusionRegex);
    });

    // Also copy the generator files (that's the compiled .js files, plus all other non-.ts files)
    const tempRoot = './tmp';
    copyRecursive(path.join(tempRoot, 'yeoman'), outputRoot, '**/*.js');
    copyRecursive(yeomanGeneratorSource, outputRoot, '**/!(*.ts)');

    // Clean up
    rimraf.sync(tempRoot);
}

function buildDotNetNewNuGetPackage() {
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
        const templateOutputDir = path.join(outputRoot, 'Content', templateName);
        writeTemplate(templateConfig.dir, templateOutputDir, contentReplacements, filenameReplacements, commonForceInclusionRegex);

        // Add the .template.config dir and its contents
        const templateConfigDir = path.join(templateOutputDir, '.template.config');
        mkdirp.sync(templateConfigDir);

        fs.writeFileSync(path.join(templateConfigDir, 'template.json'), JSON.stringify({
            author: 'Microsoft',
            classifications: ["Web", "MVC", "SPA"],
            groupIdentity: `Microsoft.AspNetCore.SpaTemplates.${templateConfig.dotNetNewId}`,
            identity: `Microsoft.AspNetCore.SpaTemplates.${templateConfig.dotNetNewId}.CSharp`,
            name: `MVC ASP.NET Core with ${templateConfig.displayName}`,
            preferNameDirectory: true,
            primaryOutputs: [{ path: `${sourceProjectName}.csproj` }],
            shortName: `${templateConfig.dotNetNewId.toLowerCase()}`,
            sourceName: sourceProjectName,
            sources: [{
                source: './',
                target: './',
                exclude: ['.deployment', '.template.config/**', 'project.json', '*.xproj', '**/_placeholder.txt']
            }],
            symbols: {
                sdkVersion: {
                    type: 'bind',
                    binding: 'dotnet-cli-version',
                    replaces: '1.0.0-preview2-1-003177'
                },
                dockerBaseImage: {
                    type: 'parameter',
                    replaces: 'microsoft/dotnet:1.1.0-sdk-projectjson',
                    defaultValue: 'microsoft/dotnet:1.1.0-sdk-msbuild'
                }
            },
            tags: { language: 'C#', type: 'project' },
        }, null, 2));

        fs.writeFileSync(path.join(templateConfigDir, 'dotnetcli.host.json'), JSON.stringify({
            symbolInfo: {}
        }, null, 2));
    });

    // Invoke NuGet to create the final package
    const yeomanPackageVersion = JSON.parse(fs.readFileSync(path.join(yeomanGeneratorSource, 'package.json'), 'utf8')).version;
    writeTemplate('./src/dotnetnew', outputRoot, [
        { from: /\{version\}/g, to: yeomanPackageVersion },
    ], [], null);
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

function runAllPrepublishScripts() {
    Object.getOwnPropertyNames(templates).forEach(templateKey => {
        const templateInfo = templates[templateKey];

        // First run standard prepublish steps
        runScripts(templateInfo.dir, commonTemplatePrepublishSteps);

        // Second, run any template-specific prepublish steps
        if (templateInfo.prepublish) {
            runScripts(templateInfo.dir, templateInfo.prepublish);
        }
    });
}

function runScripts(rootDir: string, scripts: string[]) {
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
runAllPrepublishScripts();
buildYeomanNpmPackage(yeomanOutputRoot);
const dotNetNewNupkgPath = buildDotNetNewNuGetPackage();

// Move the .nupkg file to the artifacts dir
fs.renameSync(dotNetNewNupkgPath, path.join(artifactsDir, path.basename(dotNetNewNupkgPath)));

// Finally, create a .tar.gz file containing the built generator-aspnetcore-spa.
// The CI system can treat this as the final built artifact.
// Note that the targz APIs only come in async flavor.
targz().compress(yeomanOutputRoot, path.join(artifactsDir, 'generator-aspnetcore-spa.tar.gz'), err => {
    if (err) { throw err; }
});
