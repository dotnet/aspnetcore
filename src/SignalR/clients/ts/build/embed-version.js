const path = require("path");
const fs = require("fs");
const pkg = require(path.resolve(process.cwd(), "package.json"));

const args = process.argv.slice(2);
let verbose = false;
for (let i = 0; i < args.length; i++) {
    switch (args[i]) {
        case "-v":
            verbose = true;
            break;
    }
}

function processDirectory(dir) {
    if (verbose) {
        console.log(`processing ${dir}`);
    }
    for (const item of fs.readdirSync(dir)) {
        const fullPath = path.resolve(dir, item);
        const stats = fs.statSync(fullPath);
        if (stats.isDirectory()) {
            processDirectory(fullPath);
        } else if (stats.isFile()) {
            processFile(fullPath);
        }
    }
}

const SEARCH_STRING = "0.0.0-DEV_BUILD";
function processFile(file) {
    if (file.endsWith(".js") || file.endsWith(".ts")) {
        if (verbose) {
            console.log(`processing ${file}`);
        }
        let content = fs.readFileSync(file);
        content = content.toString().replace(SEARCH_STRING, pkg.version);
        fs.writeFileSync(file, content);
    } else if (verbose) {
        console.log(`skipping ${file}`);
    }
}

processDirectory(path.resolve(process.cwd(), "dist"));