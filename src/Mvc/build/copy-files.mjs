import * as fs from 'fs';
import * as path from 'path';

const repoRoot = process.env.RepoRoot;
if (!repoRoot) {
  throw new Error('RepoRoot environment variable is not set')
}

// Search all the folders in the src directory for the files "jquery.validate.js" and "jquery.validate.min.js" but skip this
// folder as well as the "node_modules" folder, the "bin" folder, and the "obj" folder. Recurse over subfolders.

const srcDir = path.join(repoRoot, 'src');
const files = [];
const search = (dir) => {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    if (entry.isDirectory() && entry.name !== 'node_modules' && entry.name !== 'bin' && entry.name !== 'obj') {
      search(path.join(dir, entry.name));
    } else if (entry.isFile() && (entry.name === 'jquery.validate.js' || entry.name === 'jquery.validate.min.js')) {
      files.push(path.join(dir, entry.name));
    }
  }
}

search(srcDir);

// Replace the files found with the versions from <<current-folder>>/node_modules/jquery-validation/dist.
// Note that <<current-folder>>/node_modules/jquery-validation/dist/jquery.validate.js needs to override the
// jquery.validate.js file found in the files array and the same for jquery.validate.min.js.
const nodeModulesDir = path.join(import.meta.dirname, 'node_modules', 'jquery-validation', 'dist');

for (const file of files) {
  const source = path.join(nodeModulesDir, path.basename(file));
  const target = file;
  fs.copyFileSync(source, target);
  console.log(`Copied ${path.basename(file)} to ${target}`);
}
