import * as fs from 'fs';
import * as path from 'path';
import * as yeoman from 'yeoman-generator';
import * as uuid from 'node-uuid';
import * as glob from 'glob';
import * as semver from 'semver';
import * as chalk from 'chalk';
import { execSync } from 'child_process';
import npmWhich = require('npm-which');
const yosay = require('yosay');
const toPascalCase = require('to-pascal-case');
const isWindows = /^win/.test(process.platform);
const generatorPackageJson = require(path.resolve(__dirname, '../package.json'));

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

// Once everyone is on .csproj-compatible tooling, we might be able to remove the global.json files and eliminate
// this SDK choice altogether. That would be good because then it would work with whatever SDK version you have
// installed. For now, we need to specify an SDK version explicitly, because there's no support for wildcards, and
// preview3+ tooling doesn't support project.json at all.
const sdkChoices = [{
    value: '1.0.0-preview2-1-003177',   // Current released version
    name: 'project.json' + chalk.gray(' (compatible with .NET Core tooling preview 2 and Visual Studio 2015)'),
    includeFiles: [/^project.json$/, /\.xproj$/, /_placeholder.txt$/]
}, {
    value: '1.0.0-preview3-004056',     // Version that ships with VS2017RC
    name: '.csproj' + chalk.gray('      (compatible with .NET Core tooling preview 3 and Visual Studio 2017)'),
    includeFiles: [/\.csproj$/]
}];

class MyGenerator extends yeoman.Base {
    private _answers: any;
    private _optionOrPrompt: YeomanPrompt;

    constructor(args: string | string[], options: any) {
        super(args, options);
        this._optionOrPrompt = optionOrPrompt;
        this.log(yosay('Welcome to the ASP.NET Core Single-Page App generator!\n\nVersion: ' + generatorPackageJson.version));

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
        }, {
            type: 'list',
            name: 'sdkVersion',
            message: 'What type of project do you want to create?',
            choices: sdkChoices
        }], firstAnswers => {
            const frameworkChoice = templates.filter(t => t.value === firstAnswers.framework)[0];
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
                answers.framework = firstAnswers.framework;
                this._answers = answers;
                this._answers.framework = firstAnswers.framework;
                this._answers.sdkVersion = firstAnswers.sdkVersion;
                this._answers.namePascalCase = toPascalCase(answers.name);
                this._answers.projectGuid = this.options['projectguid'] || uuid.v4();
                done();
            });
        });
    }

    writing() {
        const templateRoot = this.templatePath(this._answers.framework);
        const chosenSdk = sdkChoices.filter(sdk => sdk.value === this._answers.sdkVersion)[0];
        glob.sync('**/*', { cwd: templateRoot, dot: true, nodir: true }).forEach(fn => {
            // Token replacement in filenames
            let outputFn = fn.replace(/tokenreplace\-([^\.\/]*)/g, (substr, token) => this._answers[token]);

            // Rename template_gitignore to .gitignore in output
            if (path.basename(fn) === 'template_gitignore') {
                outputFn = path.join(path.dirname(fn), '.gitignore');
            }

            // Decide whether to emit this file
            const isTestSpecificFile = testSpecificPaths.some(regex => regex.test(outputFn));
            const isSdkSpecificFile = sdkChoices.some(sdk => sdk.includeFiles.some(regex => regex.test(outputFn)));
            const matchesChosenSdk = chosenSdk.includeFiles.some(regex => regex.test(outputFn));
            const emitFile = (matchesChosenSdk || !isSdkSpecificFile)
                          && (this._answers.tests || !isTestSpecificFile);

            if (emitFile) {
                let inputFullPath = path.join(templateRoot, fn);
                let destinationFullPath = this.destinationPath(outputFn);
                let deleteInputFileAfter = false;
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
                    deleteInputFileAfter = true;
                }

                const outputDirBasename = path.basename(path.dirname(destinationFullPath));
                if (outputDirBasename === 'dist') {
                    // Don't do token replacement in 'dist' files, as they might just randomly contain
                    // sequences like '<%=' even though they aren't actually template files
                    this.fs.copy(
                        inputFullPath,
                        destinationFullPath
                    );
                } else {
                    this.fs.copyTpl(
                        inputFullPath,
                        destinationFullPath,
                        this._answers
                    );
                }

                if (deleteInputFileAfter) {
                    this.fs.delete(inputFullPath);
                }
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
        process.exit(1);
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
