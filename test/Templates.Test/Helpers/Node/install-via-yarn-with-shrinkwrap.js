/*
==============================================================================================
Implementation based on https://github.com/dabroek/shrinkwrap-to-lockfile/ by Matthijs Dabroek

License for original package:
MIT License

Copyright (c) 2017 Matthijs Dabroek

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
==============================================================================================

Modified to support execution across multiple directories simultaneously,
to provide console output while executing, and to eliminate lodash dependency.
*/

const fs = require('fs');
const path = require('path');
const execSync = require('child_process').execSync;

function parseJsonFile(filename) {
  let obj = {};

  try {
    const file = fs.readFileSync(path.resolve(process.cwd(), filename), 'utf8');
    obj = JSON.parse(file);
  } catch (error) {
    throw new Error(`${filename} could not be parsed.`);
  }

  return obj;
}

function getDependencyVersions(dependencies) {
  const result = {};
  for (const name in dependencies) {
    if (dependencies.hasOwnProperty(name)) {
      const pkg = dependencies[name];
      const version = pkg.version || pkg;
      result[name] = version.match(/(\d+\.\d+\.\d+(?:-.+)?)/)[0];
    }
  }
  return result;
}

function objectMergeLeft(a, b) {
  return _.reduce(a, (result, value, key) => {
    result[key] = value;

    if (!_.isEqual(value, b[key])) {
      result[key] = b[key];
    }

    return result;
  }, {});
}

function updatePackageJson(shrinkwrapFile, packageFile) {
  const packageFilePath = path.resolve(process.cwd(), packageFile);
  const origPackageFileContents = fs.readFileSync(packageFilePath);
  const shrinkwrapJson = parseJsonFile(shrinkwrapFile);
  const packageJson = parseJsonFile(packageFile);

  packageJson.dependencies = getDependencyVersions(shrinkwrapJson.dependencies);
  delete packageJson.devDependencies;

  fs.writeFileSync(packageFilePath, JSON.stringify(packageJson, null, 2));
  return {
    dispose: () => {
      fs.writeFileSync(packageFilePath, origPackageFileContents);
    }
  };
}

const temporaryPackageJson = updatePackageJson('npm-shrinkwrap.json', 'package.json');
try {
  console.log('Generating yarn.lock from npm-shrinkwrap.json...');
  execSync('yarn install --mutex network', { stdio: 'inherit' });
} finally {
  temporaryPackageJson.dispose();
}
