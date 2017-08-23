var Generator = require('yeoman-generator');
var fs = require('fs');
var path = require('path');
var marked = require('marked');
var TerminalRenderer = require('marked-terminal');
 
marked.setOptions({ renderer: new TerminalRenderer() });

module.exports = class extends Generator {
    run() {
        // Just display deprecation notice from README.md
        const readmePath = path.join(__dirname, '../README.md');
        console.log(marked(fs.readFileSync(readmePath, 'utf8')));
    }
};
