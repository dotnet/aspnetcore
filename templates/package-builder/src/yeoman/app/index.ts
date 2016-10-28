import * as path from 'path';
import * as yeoman from 'yeoman-generator';
import * as uuid from 'node-uuid';
import * as glob from 'glob';
const yosay = require('yosay');
const toPascalCase = require('to-pascal-case');

type YeomanPrompt = (opt: yeoman.IPromptOptions | yeoman.IPromptOptions[], callback: (answers: any) => void) => void;
const optionOrPrompt: YeomanPrompt = require('yeoman-option-or-prompt');

const templates = [
    { value: 'angular-2', name: 'Angular 2' },
    { value: 'aurelia', name: 'Aurelia' },
    { value: 'knockout', name: 'Knockout' },
    { value: 'react', name: 'React' },
    { value: 'react-redux', name: 'React with Redux' }
];

class MyGenerator extends yeoman.Base {
    private _answers: any;
    private _optionOrPrompt: YeomanPrompt;

    constructor(args: string | string[], options: any) {
        super(args, options);
        this._optionOrPrompt = optionOrPrompt;
        this.log(yosay('Welcome to the ASP.NET Core Single-Page App generator!'));
    }

    prompting() {
        const done = this.async();

        this.option('projectguid');
        this._optionOrPrompt([{
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
            this._answers.projectGuid = this.options['projectguid'] || uuid.v4();
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

            // Likewise, output template_nodemodules_placeholder.txt as node_modules/_placeholder.txt
            // This is a workaround for https://github.com/aspnet/JavaScriptServices/issues/235. We need the new project
            // to have a nonempty node_modules dir as far as *source control* is concerned. So, there's a gitignore
            // rule that explicitly causes node_modules/_placeholder.txt to be tracked in source control. But how
            // does that file get there in the first place? It's not enough for such a file to exist when the
            // generator-aspnetcore-spa NPM package is published, because NPM doesn't allow any directories called
            // node_modules to exist in the package. So we have a file with at a different location, and move it
            // to node_modules as part of executing the template.
            if (path.basename(fn) === 'template_nodemodules_placeholder.txt') {
                outputFn = path.join(path.dirname(fn), 'node_modules', '_placeholder.txt');
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
