import * as yeoman from 'yeoman-generator';
import * as glob from 'glob';
import * as gitignore from 'gitignore-parser';
import * as fs from 'fs';
import * as path from 'path';
import * as _ from 'lodash';
import * as diff from 'diff';
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

function writeCommonFiles(outDir: string) {
    let filesByTemplate = _.mapValues(templates, listFilesExcludingGitignored);
    let commonFiles = _.intersection.apply(_, _.values(filesByTemplate));
    
    commonFiles.forEach(fn => {
        let templateRoots = _.values(templates);
        let origContent = fs.readFileSync(path.join(templateRoots[0], fn));            

        if (isTextFile(fn)) {
            // For text files, we copy the portion that's common to all the templates
            let commonText = origContent.toString('utf8');
            templateRoots.slice(1).forEach(otherTemplateRoot => {
                let otherTemplateContent = fs.readFileSync(path.join(otherTemplateRoot, fn), 'utf8');
                commonText = diff.diffLines(commonText, otherTemplateContent)
                    .filter(c => !(c.added || c.removed))
                    .map(c => c.value)
                    .join('');
            });
            
            writeFileEnsuringDirExists(outDir, fn, commonText);
        } else {
            // For binary (or maybe-binary) files, we only consider them common if they are identical across all templates
            let isIdenticalEverywhere = !templateRoots.slice(1).some(otherTemplateRoot => {
                return !fs.readFileSync(path.join(otherTemplateRoot, fn)).equals(origContent);
            });
            if (isIdenticalEverywhere) {
                writeFileEnsuringDirExists(outDir, fn, origContent);
            }
        }
    });
}

function writeDiffsForTemplate(sourceRoot: string, destRoot: string, commonRoot: string) {
    listFilesExcludingGitignored(sourceRoot).forEach(fn => {
        const commonFn = path.join(commonRoot, fn);
        const sourceContent = fs.readFileSync(path.join(sourceRoot, fn));
        
        if (!fs.existsSync(commonFn)) {
            // This file is unique to this template - just copy as-is
            writeFileEnsuringDirExists(destRoot, fn, sourceContent);
        } else {
            let commonText = fs.readFileSync(commonFn, 'utf8');
            let sourceText = sourceContent.toString('utf8');
            if (commonText !== sourceText) {            
                // Write a diff vs the common version of this file
                let fileDiff = diff.createPatch(fn, commonText, sourceText, null, null);
                writeFileEnsuringDirExists(destRoot, fn + '.patch', fileDiff);
            }
        }
    });
}

const outputRoot = './generator-aspnet-spa';
const commonRoot = path.join(outputRoot, 'templates/common'); 
rimraf.sync(outputRoot);
writeCommonFiles(commonRoot);

_.forEach(templates, (templateRootDir, templateName) => {
    writeDiffsForTemplate(templateRootDir, path.join(outputRoot, 'templates', templateName), commonRoot);
});
