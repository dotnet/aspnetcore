
// Iterate over all the .razor files in Pages and replace
// https://cdnjs.cloudflare.com/ajax/libs/jquery-validate/<<current-version>>/jquery.validate.min.js
// with the new version.
// Also replace the integrity attribute with the new integrity attribute.
// The current integrity attribute and the current version can be read from a json file on this script folder
// called jquery-validate-versions.json that has the following structure:
// {
//   "currentVersion": "1.19.5",
//   "integrity": "sha256-JwUksNJ6/R07ZiLRoXbGeNrtlFZMFDKX4hemPiHOmCA=",
//   "newVersion": "1.20.1",
//   "newIntegrity": "sha256-7Z6+1q1Z2+7e5Z2e5Z2+7e5Z2+7e5Z2+7e5Z2+7e5Z2="
// }
// After we've updated the files, we'll update the json file with the new version and integrity.

// Read the JSON file
import fs from 'fs';

const jqueryValidateVersions = JSON.parse(fs.readFileSync('./jquery-validate-versions.json', 'utf8'));

// Get the current version and integrity
const currentVersion = jqueryValidateVersions.currentVersion;
const integrity = jqueryValidateVersions.integrity;

// Get the new version and integrity
const newVersion = jqueryValidateVersions.newVersion;
const newIntegrity = jqueryValidateVersions.newIntegrity;

// Iterate recursively over all the .razor files in the Pages folder
const replaceIntegrity = (dir) => {
  const files = fs.readdirSync(dir);
  files.forEach((file) => {
    const filePath = `${dir}/${file}`;
    const stat = fs.statSync(filePath);
    if (stat.isDirectory()) {
      replaceIntegrity(filePath);
    } else {
      if (filePath.endsWith('.cshtml')) {
        // Read the file
        let content = fs.readFileSync(filePath, 'utf8');
        // Replace the old version with the new version
        content = content.replace(`https://cdnjs.cloudflare.com/ajax/libs/jquery-validate/${currentVersion}/jquery.validate.min.js`, `https://cdnjs.cloudflare.com/ajax/libs/jquery-validate/${newVersion}/jquery.validate.min.js`);
        // Replace the old integrity with the new integrity
        content = content.replace(`integrity="${integrity}"`, `integrity="${newIntegrity}"`);
        // Write the file
        fs.writeFileSync(filePath, content);
      }
    }
  });
}

replaceIntegrity('./src/Areas/Identity');

// Update the JSON file with the new version and integrity
jqueryValidateVersions.currentVersion = newVersion;
jqueryValidateVersions.integrity = newIntegrity;
delete jqueryValidateVersions.newVersion;
delete jqueryValidateVersions.newIntegrity;

fs.writeFileSync('./jquery-validate-versions.json', JSON.stringify(jqueryValidateVersions, null, 2));
