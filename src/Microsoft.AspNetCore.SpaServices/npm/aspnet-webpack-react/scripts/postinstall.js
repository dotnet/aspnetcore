var fs = require('fs');
var path = require('path');
var Hjson = require('hjson');

// This logic is a workaround for #1066.
// See the comment in index.ts for details.

function findInDirOrAncestor(targetFilename, rootDir) {
    var candidateFilename = path.join(rootDir, targetFilename);
    if (fs.existsSync(candidateFilename)) {
        return candidateFilename;
    }

    var parentDir = path.join(rootDir, '..');
    return parentDir !== rootDir ? findInDirOrAncestor(targetFilename, parentDir) : null;
}

function findTsConfigFile() {
    var rootDir = path.join(__dirname, '..', '..'); // Start 2 levels up because this package has a tsconfig file of its own
    var tsConfigFile = 'tsconfig.json';
    var tsConfigFileName = findInDirOrAncestor(tsConfigFile, rootDir);
    if (!tsConfigFileName) {
        console.error('Could not locate ' + tsConfigFile + ' in ' + rootDir + ' or any ancestor directory.');
    }
    return tsConfigFileName;
}

function ensureTsConfigContainsTypesEntry(packageName) {
    var tsConfigFileName = findTsConfigFile();
    if (tsConfigFileName) {
        var parsedTsConfig = Hjson.rt.parse(fs.readFileSync(tsConfigFileName, 'utf8'));
        parsedTsConfig.compilerOptions = parsedTsConfig.compilerOptions || {};
        parsedTsConfig.compilerOptions.types = parsedTsConfig.compilerOptions.types || [];

        if (parsedTsConfig.compilerOptions.types.indexOf(packageName) < 0) {
            parsedTsConfig.compilerOptions.types.push(packageName);

            var hjsonOptions = {
                bracesSameLine: true,
                multiline: 'off',
                quotes: 'all',
                separator: true,
                space: 2
            };
            fs.writeFileSync(tsConfigFileName, Hjson.rt.stringify(parsedTsConfig, hjsonOptions), 'utf8');
        }
    }
}

try {
    ensureTsConfigContainsTypesEntry('aspnet-webpack-react');
} catch(ex) {
    console.error(ex);
    process.exit(0); // Don't break installation
}
