import * as glob from 'glob';
import * as gitignore from 'gitignore-parser';
import * as fs from 'fs';
import * as path from 'path';
import * as _ from 'lodash';
import * as mkdirp from 'mkdirp';
import * as rimraf from 'rimraf';

const textFileExtensions = ['.gitignore', '.config', '.cs', '.cshtml', 'Dockerfile', '.html', '.js', '.json', '.jsx', '.md', '.ts', '.tsx', '.xproj'];

const templates = {
    'angular-2': '../../templates/Angular2Spa/',
    'knockout': '../../templates/KnockoutSpa/',
    'react-redux': '../../templates/ReactReduxSpa/',
    'react': '../../templates/ReactSpa/'
};

const contentReplacements: { from: RegExp, to: string }[] = [
    { from: /\bWebApplicationBasic\b/g, to: '<%= namePascalCase %>' },
    { from: /<ProjectGuid>[0-9a-f\-]{36}<\/ProjectGuid>/g, to: '<ProjectGuid><%= projectGuid %></ProjectGuid>' },
    { from: /<RootNamespace>.*?<\/RootNamespace>/g, to: '<RootNamespace><%= namePascalCase %></RootNamespace>'},
    { from: /\s*<BaseIntermediateOutputPath.*?<\/BaseIntermediateOutputPath>/g, to: '' },
    { from: /\s*<OutputPath.*?<\/OutputPath>/g, to: '' },
];

const filenameReplacements: { from: RegExp, to: string }[] = [
    { from: /.*\.xproj$/, to: 'tokenreplace-namePascalCase.xproj' }
];

function isTextFile(filename: string): boolean {
    return textFileExtensions.indexOf(path.extname(filename).toLowerCase()) >= 0;
}

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

const outputRoot = './generator-aspnetcore-spa';
const outputTemplatesRoot = path.join(outputRoot, 'app/templates'); 
rimraf.sync(outputTemplatesRoot);

// Copy template files
_.forEach(templates, (templateRootDir, templateName) => {
    const outputDir = path.join(outputTemplatesRoot, templateName);
    writeTemplate(templateRootDir, outputDir);
});

// Also copy the generator files (that's the compiled .js files, plus all other non-.ts files)
const tempRoot = './tmp';
copyRecursive(path.join(tempRoot, 'generator'), outputRoot, '**/*.js');
copyRecursive('./src/generator', outputRoot, '**/!(*.ts)');

// Clean up
rimraf.sync(tempRoot);
