import * as path from 'path';
import * as yeoman from 'yeoman-generator';
import * as uuid from 'node-uuid';
import * as glob from 'glob';
const yosay = require('yosay');
const toPascalCase = require('to-pascal-case');

const templates = [
    { value: 'angular-2', name: 'Angular 2' },
    { value: 'knockout', name: 'Knockout' },
    { value: 'react', name: 'React' },
    { value: 'react-redux', name: 'React with Redux' }
];

class MyGenerator extends yeoman.Base {
    private _answers: any;

    constructor(args: string | string[], options: any) {
        super(args, options);
        this.log(yosay('Welcome to the ASP.NET Core Single-Page App generator!'));
    }

    prompting() {
        const done = this.async();

        this.prompt([{
            type: 'list',
            name: 'framework',
            message: 'Framework',
            choices: templates
        }, {
            type: 'input',
            name: 'name',
            message: 'Your project name',
            default: this.appname
        }], answers => {
            this._answers = answers;
            this._answers.namePascalCase = toPascalCase(answers.name);
            this._answers.projectGuid = uuid.v4();
            done();
        });
    }

    writing() {
        var templateRoot = this.templatePath(this._answers.framework);
        glob.sync('**/*', { cwd: templateRoot, dot: true, nodir: true }).forEach(fn => {
            // Token replacement in filenames
            let outputFn = fn.replace(/tokenreplace\-([^\.\/]*)/g, (substr, token) => this._answers[token]);

            // Rename template_gitignore to .gitignore in output
            if (path.basename(fn) === 'template_gitignore') {
                outputFn = path.join(path.dirname(fn), '.gitignore');
            }

            this.fs.copyTpl(
                path.join(templateRoot, fn),
                this.destinationPath(outputFn),
                this._answers
            );
        });
    }

    installingDeps() {
        this.installDependencies({
            npm: true,
            bower: false,
            callback: () => {
                this.spawnCommandSync('dotnet', ['restore']);
                this.spawnCommandSync('./node_modules/.bin/webpack', ['--config', 'webpack.config.vendor.js']);
                this.spawnCommandSync('./node_modules/.bin/webpack');
            }
        });
    }
}

declare var module: any;
(module).exports = MyGenerator;
