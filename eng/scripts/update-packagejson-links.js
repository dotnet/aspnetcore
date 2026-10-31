const path = require("path");
const fs = require("fs");

const args = process.argv.slice(2);
const packageJson = args[0];
const packageVersion = args[1];

const fileContent = fs.readFileSync(packageJson);
const updatedContent = fileContent.toString().replace(/\"link:.*\"/g, `">=${packageVersion}"`);
if (fileContent != updatedContent) {
    fs.writeFileSync(packageJson, updatedContent);
}
