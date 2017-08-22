import * as childProcess from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import * as rimraf from 'rimraf';
import * as mkdirp from 'mkdirp';

const templatePackageName = 'Microsoft.DotNet.Web.Spa.ProjectTemplates';
const templatePackageArtifactsDir = '../templates/package-builder/dist/artifacts';

export function generateProjectSync(targetDir: string, templateName: string) {
    installTemplatePackage(targetDir, templatePackageName, templateName);
    executeDotNetNewTemplateSync(targetDir, templateName);
    executeCommand('npm install', /* quiet */ false, targetDir);
}

function installTemplatePackage(targetDir: string, packageName: string, templateName: string) {
    // First figure out which file is the latest one for this package
    const packagePaths = fs.readdirSync(templatePackageArtifactsDir)
        .filter(name => name.startsWith(templatePackageName + '.'))
        .filter(name => path.extname(name) === '.nupkg')
        .map(name => path.join(templatePackageArtifactsDir, name))
        .sort();
    const latestPackagePath = packagePaths[packagePaths.length - 1];

    if (!latestPackagePath) {
        throw new Error(`Could not find ${packageName}.*.nupkg in directory ${templatePackageArtifactsDir}`);
    }

    // Uninstall any older version so we can be sure the new one did install
    try {
        console.log(`Uninstalling any prior version of ${packageName}...`);
        executeCommand(`dotnet new --uninstall ${packageName}`, /* quiet */ true);
    } catch (ex) {
        // Either no prior version existed, or we failed to uninstall. We'll determine
        // which it was next. 
    }
    try {
        console.log(`Verifying that no prior version of ${packageName} is still installed...`);
        executeDotNetNewTemplateSync(targetDir, templateName, /* quiet */ true);
        throw new Error(`Failed to uninstall template package ${packageName}. The template '${templateName}' was not removed as expected.`);
    } catch (ex) {
        // Looks like we successfully uninstalled it
        console.log(`Confirmed that no prior version of ${templatePackageName} remains installed.`);
    }

    // Now install the new version
    console.log(`Installing new templates package at ${latestPackagePath}...`);
    executeCommand(`dotnet new --install ${latestPackagePath}`, /* quiet */ true);
}

function executeDotNetNewTemplateSync(targetDir: string, templateName: string, quiet?: boolean) {
    rimraf.sync(targetDir);
    mkdirp.sync(targetDir);
    executeCommand(`dotnet new ${templateName}`, quiet, targetDir);
}

function executeCommand(command: string, quiet?: boolean, cwd?: string) {
    childProcess.execSync(command, {
        cwd,
        stdio: quiet ? null : 'inherit',
        encoding: 'utf8'
    });
}
