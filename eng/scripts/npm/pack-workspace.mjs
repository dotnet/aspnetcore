// Handles packing of the packages in the workspace
// The script iterates over all public packages in the workspace, updates their versions, and then packs them into tarballs.
import { resolve, dirname } from 'path';
import { execSync } from 'child_process';
import fsExtra from 'fs-extra';
const { existsSync, writeJsonSync, readJsonSync, moveSync, copySync, removeSync } = fsExtra;
import { applyVersions } from './update-dependency-versions.mjs';

// Valid actions are --update-versions and --create-packages
const action = process.argv[2];

const workspacePath = process.argv[3];

const defaultPackageVersion = process.argv[4];

const packageOutputPath = process.argv[5];

const intermediateOutputPath = process.argv[6];

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

if (!intermediateOutputPath) {
  throw new Error('The intermediate output path was not provided.');
}

// Log all the captured process arguments
console.log(`Workspace Path: ${workspacePath}`);
console.log(`Default Package Version: ${defaultPackageVersion}`);
console.log(`Package Output Path: ${packageOutputPath}`);
console.log(`Intermediate Output Path: ${intermediateOutputPath}`);

if (action === '--update-versions') {
  const [packagesToPack, renames] = applyVersions(defaultPackageVersion, workspacePath);
  // Write packagesToPack and renames to a file
  writeJsonSync(resolve(intermediateOutputPath, 'packagesToPack.json'), packagesToPack);
  writeJsonSync(resolve(intermediateOutputPath, 'renames.json'), renames);
} else if (action === '--create-packages') {
  if (!existsSync(packageOutputPath)) {
    throw new Error(`The package output path ${packageOutputPath} does not exist.`);
  }

  // Read packagesToPack and renames from a file
  const packagesToPack = readJsonSync(resolve(intermediateOutputPath, 'packagesToPack.json'));
  const renames = readJsonSync(resolve(intermediateOutputPath, 'renames.json'));
  try {
    createPackages(packagesToPack);
  } finally {
    // Restore the package.json files to their "originals"
    for (const [from, to] of renames) {
      moveSync(from, to, { overwrite: true });
    }
  }
} else {
  throw new Error(`The action ${action} is not supported.`);
}

// For each package to pack run npm pack
function createPackages(packagesToPack) {
  // The repo's LICENSE file lives at the root of the workspace next to package.json.
  const licenseSourcePath = resolve(dirname(workspacePath), 'LICENSE.txt');
  if (!existsSync(licenseSourcePath)) {
    throw new Error(`The license file ${licenseSourcePath} does not exist.`);
  }

  for (const [packagePath, packageJson] of packagesToPack) {
    const packageName = packageJson.name;
    const packageVersion = defaultPackageVersion;
    const packageDir = dirname(packagePath);
    const normalizedPackageName = packageName.replace('@', '').replace('/', '-');
    const packageFileName = `${normalizedPackageName}-${packageVersion}.tgz`;
    const packageTarball = resolve(packageDir, `${packageFileName}`);

    // Copy the repo's LICENSE file into the package directory so that it ships in the published
    // package. npm always includes a top-level LICENSE file in the tarball, even when the
    // package.json "files" allowlist is specified, so no changes to "files" are required.
    const licenseDestPath = resolve(packageDir, 'LICENSE.txt');
    const licenseAlreadyPresent = existsSync(licenseDestPath);
    if (!licenseAlreadyPresent) {
      copySync(licenseSourcePath, licenseDestPath);
    }

    try {
      console.log(`Packing ${packageName}...`);
      // Log and execute the command
      console.log(`npm pack "${packageDir}" --pack-destination "${packageOutputPath}"`);
      execSync(`npm pack "${packageDir}" --pack-destination "${packageOutputPath}"`, { stdio: 'inherit' });

      console.log(`Packed ${packageName} to ${resolve(packageOutputPath, packageTarball)}`);
    } finally {
      // Remove the copied LICENSE file so we don't leave build artifacts in the source tree.
      if (!licenseAlreadyPresent) {
        removeSync(licenseDestPath);
      }
    }
  }
}

