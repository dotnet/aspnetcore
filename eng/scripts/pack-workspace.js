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

if (!fs.existsSync(packageOutputPath)) {
  throw new Error(`The package output path ${packageOutputPath} does not exist.`);
}

const packages = workspacePackage.workspaces;
const packagesToPack = [];
for (const package of packages) {
  const packagePath = path.resolve(workspaceDir, package, 'package.json');
  const packageJson = JSON.parse(fs.readFileSync(packagePath, 'utf-8'));
  const { execSync } = require('child_process');
  if (!packageJson.private) {
    packagesToPack.push([packagePath, packageJson]);
  } else {
    console.log(`Skipping ${packageJson.name} because it is marked as private.`);
  }
}

const currentDir = process.cwd();
const renames = [];

try {
  // For each package to be packed, run npm version to apply the version
  applyPackageVersion(packagesToPack);

  updateDependencyVersions(packagesToPack);

  createPackages(packagesToPack);
} finally {
  // Restore the package.json files to their "originals"
  for (const [from, to] of renames) {
    fs.moveSync(from, to, { overwrite: true });
  }
}

function applyPackageVersion(packagesToPack) {
  for (const [packagePath, packageJson] of packagesToPack) {
    const packageName = packageJson.name;
    const packageVersion = defaultPackageVersion;
    const packageDir = path.dirname(packagePath);
    // Run npm version packageVersion --no-git-tag-version
    // This will update the package.json version to the specified version without creating a git tag
    // Make a backup of the package.json
    fs.copyFileSync(packagePath, `${packagePath}.bak`);
    renames.push([`${packagePath}.bak`, packagePath]);

    process.chdir(packageDir);
    execSync(`npm version ${packageVersion} --no-git-tag-version`, { stdio: 'inherit' });
    process.chdir(currentDir);
    console.log(`Applied version ${packageVersion} to ${packageName} in ${packageDir}...`);
  }
}

// For each package to pack run npm pack
function createPackages(packagesToPack) {
  for (const [packagePath, packageJson] of packagesToPack) {
    const packageName = packageJson.name;
    const packageVersion = defaultPackageVersion;
    const packageDir = path.dirname(packagePath);
    const normalizedPackageName = packageName.replace('@', '').replace('/', '-');
    const packageFileName = `${normalizedPackageName}-${packageVersion}.tgz`;
    const packageTarball = path.resolve(packageDir, `${packageFileName}`);
    console.log(`Packing ${packageName}...`);
    // Log and execute the command
    console.log(`npm pack ${packageDir} --pack-destination ${packageOutputPath}`);
    execSync(`npm pack ${packageDir} --pack-destination ${packageOutputPath}`, { stdio: 'inherit' });

    console.log(`Packed ${packageName} to ${path.resolve(packageOutputPath, packageTarball)}`);
  }
}

// For each package to be packed, update the version for the dependencies that are part of the workspace.
// The packagesToPack are sorted in topological order, which means that the dependencies for a package, will always
// be listed before it in the packagesToPack array.
// We can't use the package.json files to get the dependencies because they have been updated by the npm version command.
// We need to load them from disk instead.
function updateDependencyVersions(packagesToPack) {
  const seenPackagesAndVersions = [];
  for (const [packagePath, _] of packagesToPack) {
    const packageDir = path.dirname(packagePath);
    // Resolve the package again with its contents as they've been updated by the npm version command
    const packageJsonPath = path.resolve(packageDir, 'package.json');
    const packageJson = JSON.parse(fs.readFileSync(packageJsonPath, 'utf-8'));
    seenPackagesAndVersions.push([packagePath, packageJson]);
    const dependencies = packageJson.dependencies;
    let modified = false;
    console.log(`Updating dependencies for package ${packageJson.name} with version ${packageJson.version}...`);
    if (dependencies) {
      for (const dependency of Object.keys(dependencies)) {
        // Find the dependency in packagesToPack, load the package.json and update the dependency version
        const dependencyPackage = seenPackagesAndVersions.find(([_, packageJson]) => packageJson.name === dependency);
        if (dependencyPackage) {
          modified = true;
          const dependencyPackagePath = dependencyPackage[0];
          const dependencyPackageJson = JSON.parse(fs.readFileSync(dependencyPackagePath, 'utf-8'));
          dependencies[dependency] = `>=${dependencyPackageJson.version}`;
          console.log(`Updated dependency ${dependency} to ${dependencyPackageJson.version} in ${packageJson.name}.`);
        }
      }
      if (modified) {
        // Write updated package.json to disk
        fs.writeFileSync(packageJsonPath, JSON.stringify(packageJson, null, 2));
        console.log(`Updated package ${packageJson.name} dependencies.`);
      }
    }
  }
}

