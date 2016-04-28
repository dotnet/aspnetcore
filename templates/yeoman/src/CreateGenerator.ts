import * as glob from 'glob';
import * as gitignore from 'gitignore-parser';
import * as fs from 'fs';
import * as path from 'path';
import * as _ from 'lodash';
import * as mkdirp from 'mkdirp';
import * as rimraf from 'rimraf';

const textFileExtensions = ['.gitignore', '.config', '.cs', '.cshtml', 'Dockerfile', '.html', '.js', '.json', '.jsx', '.md', '.ts', '.tsx'];

const templates = {
    'angular-2': '../../templates/Angular2Spa/',
    'knockout': '../../templates/KnockoutSpa/',
    'react-redux': '../../templates/ReactReduxSpa/',
    'react': '../../templates/ReactSpa/'
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
    let gitignoreEvaluator = gitignore.compile(fs.readFileSync(path.join(root, '.gitignore'), 'utf8'));
    return glob.sync('**/*', { cwd: root, dot: true, nodir: true })
        .filter(fn => gitignoreEvaluator.accepts(fn));
}

function writeTemplate(sourceRoot: string, destRoot: string) {
    listFilesExcludingGitignored(sourceRoot).forEach(fn => {
        const sourceContent = fs.readFileSync(path.join(sourceRoot, fn));        
        writeFileEnsuringDirExists(destRoot, fn, sourceContent);
    });    
}

const outputRoot = './generator-aspnet-spa';
const commonRoot = path.join(outputRoot, 'templates/common'); 
rimraf.sync(outputRoot);

_.forEach(templates, (templateRootDir, templateName) => {
    const outputDir = path.join(outputRoot, 'templates', templateName);
    writeTemplate(templateRootDir, outputDir);
});
