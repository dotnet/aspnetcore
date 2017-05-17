import * as childProcess from 'child_process';
import * as path from 'path';
import * as rimraf from 'rimraf';
import * as mkdirp from 'mkdirp';

const generatorDirRelative = '../templates/package-builder/dist/generator-aspnetcore-spa';
const yoPackageDirAbsolute = path.resolve('./node_modules/yo');

export interface GeneratorOptions {
    framework: string;
    name: string;
    tests?: boolean;
}

export function generateProjectSync(targetDir: string, generatorOptions: GeneratorOptions) {
    const generatorDirAbsolute = path.resolve(generatorDirRelative);
    console.log(`Running NPM install to prepare Yeoman generator at ${ generatorDirAbsolute }`);
    childProcess.execSync(`npm install`, { stdio: 'inherit', cwd: generatorDirAbsolute });

    console.log(`Ensuring empty output directory at ${ targetDir }`);
    rimraf.sync(targetDir);
    mkdirp.sync(targetDir);

    const yoExecutableAbsolute = findYeomanCliScript();
    console.log(`Will invoke Yeoman at ${ yoExecutableAbsolute } to generate application in ${ targetDir } with options:`);
    console.log(JSON.stringify(generatorOptions, null, 2));
    const command = `node "${ yoExecutableAbsolute }" "${ path.resolve(generatorDirAbsolute, './app/index.js') }"`;
    const args = makeYeomanCommandLineArgs(generatorOptions);
    childProcess.execSync(`${ command } ${ args }`, {
        stdio: 'inherit',
        cwd: targetDir
    });
}

function findYeomanCliScript() {
    // On Windows, you can't invoke ./node_modules/.bin/yo from the shell for some reason.
    // So instead, we'll locate the CLI entrypoint that yeoman would expose if it was installed globally.
    const yeomanPackageJsonPath = path.join(yoPackageDirAbsolute, './package.json');
    const yeomanPackageJson = require(yeomanPackageJsonPath);
    const yeomanCliScriptRelative = yeomanPackageJson.bin.yo;
    if (!yeomanCliScriptRelative) {
        throw new Error(`Could not find Yeoman CLI script. Looked for a bin/yo entry in ${ yeomanPackageJsonPath }`);
    }

    return path.join(yoPackageDirAbsolute, yeomanCliScriptRelative);
}

function makeYeomanCommandLineArgs(generatorOptions: GeneratorOptions) {
    return Object.getOwnPropertyNames(generatorOptions)
        .map(key => `--${ key }="${ generatorOptions[key] }"`)
        .join(' ');
}
