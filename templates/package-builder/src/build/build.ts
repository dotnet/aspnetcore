import * as glob from 'glob';
import * as gitignore from 'gitignore-parser';
import * as fs from 'fs';
import * as path from 'path';
import * as _ from 'lodash';
import * as mkdirp from 'mkdirp';
import * as rimraf from 'rimraf';
import * as childProcess from 'child_process';

const isWindows = /^win/.test(process.platform);
const textFileExtensions = ['.gitignore', 'template_gitignore', '.config', '.cs', '.cshtml', 'Dockerfile', '.html', '.js', '.json', '.jsx', '.md', '.nuspec', '.ts', '.tsx', '.xproj'];
const yeomanGeneratorSource = './src/yeoman';

const templates: { [key: string]: { dir: string, dotNetNewId: string, displayName: string } } = {
    'angular-2': { dir: '../../templates/Angular2Spa/', dotNetNewId: 'Angular', displayName: 'Angular 2' },
    'knockout': { dir: '../../templates/KnockoutSpa/', dotNetNewId: 'Knockout', displayName: 'Knockout.js' },
    'react-redux': { dir: '../../templates/ReactReduxSpa/', dotNetNewId: 'ReactRedux', displayName: 'React.js and Redux' },
    'react': { dir: '../../templates/ReactSpa/', dotNetNewId: 'React', displayName: 'React.js' }
};

function isTextFile(filename: string): boolean {
    return textFileExtensions.indexOf(path.extname(filename).toLowerCase()) >= 0;
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

function writeTemplate(sourceRoot: string, destRoot: string, contentReplacements: { from: RegExp, to: string }[], filenameReplacements: { from: RegExp, to: string }[]) {
    listFilesExcludingGitignored(sourceRoot).forEach(fn => {
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

function buildYeomanNpmPackage() {
    const outputRoot = './dist/generator-aspnetcore-spa';
    const outputTemplatesRoot = path.join(outputRoot, 'app/templates');
    rimraf.sync(outputTemplatesRoot);

    // Copy template files
    const filenameReplacements = [
        { from: /.*\.xproj$/, to: 'tokenreplace-namePascalCase.xproj' }
    ];
    const contentReplacements = [
        { from: /\bWebApplicationBasic\b/g, to: '<%= namePascalCase %>' },
        { from: /<ProjectGuid>[0-9a-f\-]{36}<\/ProjectGuid>/g, to: '<ProjectGuid><%= projectGuid %></ProjectGuid>' },
        { from: /<RootNamespace>.*?<\/RootNamespace>/g, to: '<RootNamespace><%= namePascalCase %></RootNamespace>'},
        { from: /\s*<BaseIntermediateOutputPath.*?<\/BaseIntermediateOutputPath>/g, to: '' },
        { from: /\s*<OutputPath.*?<\/OutputPath>/g, to: '' },
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

function buildDotNetNewNuGetPackage() {
    const outputRoot = './dist/dotnetnew';
    rimraf.sync(outputRoot);

    // Copy template files
    const sourceProjectName = 'WebApplicationBasic';
    const projectGuid = '00000000-0000-0000-0000-000000000000';
    const filenameReplacements = [
        { from: /.*\.xproj$/, to: `${sourceProjectName}.xproj` },
        { from: /\btemplate_gitignore$/, to: '.gitignore' }
    ];
    const contentReplacements = [
        { from: /<ProjectGuid>[0-9a-f\-]{36}<\/ProjectGuid>/g, to: `<ProjectGuid>${projectGuid}</ProjectGuid>` },
        { from: /<RootNamespace>.*?<\/RootNamespace>/g, to: `<RootNamespace>${sourceProjectName}</RootNamespace>`},
        { from: /\s*<BaseIntermediateOutputPath.*?<\/BaseIntermediateOutputPath>/g, to: '' },
        { from: /\s*<OutputPath.*?<\/OutputPath>/g, to: '' },
    ];
    _.forEach(templates, (templateConfig, templateName) => {
        const templateOutputDir = path.join(outputRoot, 'templates', templateName);
        const templateOutputProjectDir = path.join(templateOutputDir, sourceProjectName);
        writeTemplate(templateConfig.dir, templateOutputProjectDir, contentReplacements, filenameReplacements);

        // Add a .netnew.json file
        fs.writeFileSync(path.join(templateOutputDir, '.netnew.json'), JSON.stringify({
            author: 'Microsoft',
            classifications: [ 'Standard>>Quick Starts' ],
            name: `ASP.NET Core SPA with ${templateConfig.displayName}`,
            groupIdentity: `Microsoft.AspNetCore.Spa.${templateConfig.dotNetNewId}`,
            identity: `Microsoft.AspNetCore.Spa.${templateConfig.dotNetNewId}`,
            shortName: `aspnetcorespa-${templateConfig.dotNetNewId.toLowerCase()}`,
            tags: { language: 'C#' },
            guids: [ projectGuid ],
            sourceName: sourceProjectName
        }, null, 2));
    });

    // Invoke NuGet to create the final package
    const yeomanPackageVersion = JSON.parse(fs.readFileSync(path.join(yeomanGeneratorSource, 'package.json'), 'utf8')).version;
    writeTemplate('./src/dotnetnew', outputRoot, [
        { from: /\{version\}/g, to: yeomanPackageVersion },
    ], []);
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
}

buildYeomanNpmPackage();
buildDotNetNewNuGetPackage();
