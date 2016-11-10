import * as fs from 'fs';
import * as path from 'path';
import * as yeoman from 'yeoman-generator';
import * as uuid from 'node-uuid';
import * as glob from 'glob';
import * as semver from 'semver';
import { execSync } from 'child_process';
import npmWhich = require('npm-which');
const yosay = require('yosay');
const toPascalCase = require('to-pascal-case');
const isWindows = /^win/.test(process.platform);

// Paths matching these regexes will only be included if the user wants tests
const testSpecificPaths = [
    /\.spec.ts$/,               // Files ending '.spec.ts'
    /(^|\/|\\)test($|\/|\\)/    // Files under any directory called 'test'
];

// These NPM dependencies will only be included if the user wants tests
const testSpecificNpmPackages = [
    "@types/chai",
    "@types/jasmine",
    "chai",
    "jasmine-core",
    "karma",
    "karma-chai",
    "karma-chrome-launcher",
    "karma-cli",
    "karma-jasmine",
    "karma-webpack"
];

type YeomanPrompt = (opt: yeoman.IPromptOptions | yeoman.IPromptOptions[], callback: (answers: any) => void) => void;
const optionOrPrompt: YeomanPrompt = require('yeoman-option-or-prompt');

const templates = [
    { value: 'angular-2', name: 'Angular 2', tests: true },
    { value: 'aurelia', name: 'Aurelia', tests: false },
    { value: 'knockout', name: 'Knockout', tests: false },
    { value: 'react', name: 'React', tests: false },
    { value: 'react-redux', name: 'React with Redux', tests: false }
];

class MyGenerator extends yeoman.Base {
    private _answers: any;
    private _optionOrPrompt: YeomanPrompt;

    constructor(args: string | string[], options: any) {
        super(args, options);
        this._optionOrPrompt = optionOrPrompt;
        this.log(yosay('Welcome to the ASP.NET Core Single-Page App generator!'));

        if (isWindows) {
            assertNpmVersionIsAtLeast('3.0.0');
        }
    }

    prompting() {
        this.option('projectguid');

        const done = this.async();
        this._optionOrPrompt([{
            type: 'list',
            name: 'framework',
            message: 'Framework',
            choices: templates
        }], frameworkAnswer => {
            const frameworkChoice = templates.filter(t => t.value === frameworkAnswer.framework)[0];
            const furtherQuestions = [{
                type: 'input',
                name: 'name',
                message: 'Your project name',
                default: this.appname
            }];

            if (frameworkChoice.tests) {
                furtherQuestions.unshift({
                    type: 'confirm',
                    name: 'tests',
                    message: 'Do you want to include unit tests?',
                    default: true as any
                });
            }

            this._optionOrPrompt(furtherQuestions, answers => {
                answers.framework = frameworkAnswer.framework;
                this._answers = answers;
                this._answers.framework = frameworkAnswer.framework;
                this._answers.namePascalCase = toPascalCase(answers.name);
                this._answers.projectGuid = this.options['projectguid'] || uuid.v4();
                done();
            });
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

            // Exclude test-specific files (unless the user has said they want tests)
            const isTestSpecificFile = testSpecificPaths.some(regex => regex.test(outputFn));
            if (this._answers.tests || !isTestSpecificFile) {
                let inputFullPath = path.join(templateRoot, fn);
                let destinationFullPath = this.destinationPath(outputFn);
                if (path.basename(fn) === 'package.json') {
                    // Special handling for package.json, because we rewrite it dynamically
                    const tempPath = destinationFullPath + '.tmp';
                    this.fs.writeJSON(
                        tempPath,
                        rewritePackageJson(JSON.parse(fs.readFileSync(inputFullPath, 'utf8')), this._answers.tests),
                        /* replacer */ null,
                        /* space */ 2
                    );
                    inputFullPath = tempPath;
                }

                this.fs.copyTpl(
                    inputFullPath,
                    destinationFullPath,
                    this._answers
                );
            }
        });
    }

    installingDeps() {
        // If available, restore dependencies using Yarn instead of NPM
        const yarnPath = getPathToExecutable('yarn');
        if (!!yarnPath) {
            this.log('Will restore NPM dependencies using \'yarn\' installed at ' + yarnPath);
            this.npmInstall = (pkgs, options, cb) => {
                return (this as any).runInstall(yarnPath, pkgs, options, cb);
            };
        }

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

function getPathToExecutable(executableName: string) {
    try {
        return npmWhich(__dirname).sync(executableName);
    } catch(ex) {
        return null;
    }
}

function assertNpmVersionIsAtLeast(minVersion: string) {
    const runningVersion = execSync('npm -v').toString();
    if (!semver.gte(runningVersion, minVersion, /* loose */ true)) {
        console.error(`This generator requires NPM version ${minVersion} or later. You are running NPM version ${runningVersion}`);
        process.exit(0);
    }
}

function rewritePackageJson(contents, includeTests) {
    if (!includeTests) {
        // Delete any test-specific packages from dependencies and devDependencies
        ['dependencies', 'devDependencies'].forEach(dependencyListName => {
            var packageList = contents[dependencyListName];
            if (packageList) {
                testSpecificNpmPackages.forEach(packageToRemove => {
                    delete packageList[packageToRemove];
                });

                if (Object.getOwnPropertyNames(packageList).length === 0) {
                    delete contents[dependencyListName];
                }
            }
        });

        // Delete any script called 'test'
        const scripts = contents.scripts;
        if (scripts && scripts.test) {
            delete scripts.test;
            if (Object.getOwnPropertyNames(scripts).length === 0) {
                delete contents.scripts;
            }
        }
    }

    return contents;
}

declare var module: any;
(module).exports = MyGenerator;
