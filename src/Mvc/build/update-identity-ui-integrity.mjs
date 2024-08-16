// Get jquery.validate.min.js from node_modules/jquery-validation/dist/ and compute its sha256 integrity attribute
// Get the version from node_modules/jquery-validation/package.json
// Update <<RepoRoot>>/src/Identity/UI/jquery-validate-versions.json with the version and integrity attribute
// by adding the newVersion and newIntegrity properties to the top level object.

import crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';

const repoRoot = process.env.RepoRoot;
if (!repoRoot) {
  throw new Error('RepoRoot environment variable is not set')
}

// Get the version from node_modules/jquery-validation/package.json
const packageJson = JSON.parse(fs.readFileSync(path.join(import.meta.dirname, 'node_modules', 'jquery-validation', 'package.json')));
const newVersion = packageJson.version;

// Get jquery.validate.min.js from node_modules/jquery-validation/dist/ and compute its sha256 integrity attribute
const nodeModulesDir = path.join(import.meta.dirname, 'node_modules', 'jquery-validation', 'dist');
const source = path.join(nodeModulesDir, 'jquery.validate.min.js');
// Compute Base64(SHA256(jquery.validate.min.js bytes))
const sha256Hash = crypto.createHash('sha256').update(fs.readFileSync(source)).digest('base64');
console.log(`Computed integrity hash for jquery.validate.min.js: sha256-${sha256Hash}`);

// Update <<RepoRoot>>/src/Identity/UI/jquery-validate-versions.json with the version and integrity attribute
const jqueryValidateVersionsFile = path.join(repoRoot, 'src', 'Identity', 'UI', 'jquery-validate-versions.json');
const jqueryValidateVersions = JSON.parse(fs.readFileSync(jqueryValidateVersionsFile));
jqueryValidateVersions.newVersion = newVersion;
jqueryValidateVersions.newIntegrity = `sha256-${sha256Hash}`;
fs.writeFileSync(jqueryValidateVersionsFile, JSON.stringify(jqueryValidateVersions, null, 2));
console.log(`Updated ${jqueryValidateVersionsFile} with new version and integrity hash`);
