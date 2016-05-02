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
            
            this.fs.copyTpl(
                path.join(templateRoot, fn),
                this.destinationPath(outputFn),
                this._answers
            );
        });
    }
    
    installingDeps() {
        // Strictly speaking, with DNX, the 'prepare' script runs 'npm install' anyway so we don't really need
        // to run it from here. But it will be needed with RC2, and it doesn't make the setup take much longer
        // because the second run is almost a no-op, so it's OK to leave it here for now.
        this.installDependencies({
            npm: true,
            bower: false,
            callback: () => {
                this.spawnCommandSync('dnu', ['restore']);

                // With DNX, the 'prepare' script builds the vendor files automatically so the following is not
                // required. With RC2 and the 'dotnet' tooling, that won't happen automatically, so the following
                // will be required:
                // this.spawnCommandSync('./node_modules/.bin/webpack', ['--config', 'webpack.config.vendor.js']);

                this.spawnCommandSync('./node_modules/.bin/webpack');                
            }
        });
    }
}

declare var module: any;
(module).exports = MyGenerator;
