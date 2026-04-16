import * as fs from 'fs';
import * as path from 'path';

const repoRoot = process.env.RepoRoot;
if (!repoRoot) {
  throw new Error('RepoRoot environment variable is not set')
}

// Search recursively over all the folders in the src directory for the files "jquery.validate.js" and "jquery.validate.min.js" except for node_modules, bin, and obj

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

// Replace each found file with the file of the same name that we downloaded during install and that is located in node_modules/jquery-validation/dist/
const nodeModulesDir = path.join(import.meta.dirname, 'node_modules', 'jquery-validation', 'dist');

for (const file of files) {
  const source = path.join(nodeModulesDir, path.basename(file));
  const target = file;
  fs.copyFileSync(source, target);
  console.log(`Copied ${path.basename(file)} to ${target}`);
}
