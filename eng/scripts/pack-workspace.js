const path = require('path');
const { execSync } = require('child_process');
const fs = require('fs-extra');

// Get the path to the workspace package.json from the process arguments list
const workspacePath = process.argv[2];
const workspacePackage = require(workspacePath);

// Get the package version from the process arguments list
const defaultPackageVersion = process.argv[3];

// Get the package output path from the process arguments list
const packageOutputPath = process.argv[4];

// Get the workspace directory
const workspaceDir = path.dirname(workspacePath);

// Validate and throw if the arguments are not provided
if (!workspacePath) {
  throw new Error('The workspace path was not provided.');
}

if (!defaultPackageVersion) {
  throw new Error('The default package version was not provided.');
}

if (!packageOutputPath) {
  throw new Error('The package output path was not provided.');
}

// Log all the captured process arguments
console.log(`Workspace Path: ${workspacePath}`);
console.log(`Default Package Version: ${defaultPackageVersion}`);
console.log(`Package Output Path: ${packageOutputPath}`);

if(!fs.existsSync(packageOutputPath)) {
  throw new Error(`The package output path ${packageOutputPath} does not exist.`);
}

const packages = workspacePackage.workspaces;
const packagesToPack = [];
for (const package of packages) {
  const packagePath = path.resolve(workspaceDir, package, 'package.json');
  const packageJson = require(packagePath);
const { execSync } = require('child_process');
  if (!packageJson.private) {
    packagesToPack.push([packagePath, packageJson]);
  } else {
    console.log(`Skipping ${packageJson.name} because it is marked as private.`);
  }
}

const currentDir = process.cwd();

// For each package to be packed, run npm pack
for (const [packagePath, packageJson] of packagesToPack) {
  const packageName = packageJson.name;
  const packageVersion = defaultPackageVersion;
  const packageDir = path.dirname(packagePath);
  const normalizedPackageName = packageName.replace('@', '').replace('/', '-');
  const packageFileName = `${normalizedPackageName}-${packageVersion}.tgz`;
  const packageTarball = path.resolve(packageDir, `${packageFileName}`);
  console.log(`Packing ${packageName}...`);
  try {
    // Run npm version packageVersion --no-git-tag-version
    // This will update the package.json version to the specified version without creating a git tag
    // Make a backup of the package.json
    fs.copyFileSync(`${packagePath}`, `${packagePath}.bak`);
    process.chdir(packageDir);
    execSync(`npm version ${packageVersion} --no-git-tag-version`, { stdio: 'inherit' });
    process.chdir(currentDir);

    // Log and execute the command
    console.log(`npm pack ${packageDir} --pack-destination ${packageOutputPath}`);
    execSync(`npm pack ${packageDir} --pack-destination ${packageOutputPath}`, { stdio: 'inherit' });

    console.log(`Packed ${packageName} to ${path.resolve(packageOutputPath, packageTarball)}`);
  } finally {
    // Restore the package.json from the backup
    fs.renameSync(`${packagePath}.bak`, `${packagePath}`);
  }
}
