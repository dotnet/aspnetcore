// -------------
// No need to invoke this directly. To run a build, execute:
//   npm run prepublish
// -------------

var Builder = require('systemjs-builder');
var builder = new Builder('./');
builder.config({
    defaultJSExtensions: true,
    paths: {
        'angular2-aspnet': 'dist/Exports',
        'angular2-aspnet/*': 'dist/*'
    },
    meta: {
        'angular2/*': { build: false },
        'rxjs/*': { build: false }
    }
});

var entryPoint = 'dist/Exports';
var tasks = [
    builder.bundle(entryPoint, './bundles/angular2-aspnet.js'),
    builder.bundle(entryPoint, './bundles/angular2-aspnet.min.js', { minify: true })
];

Promise.all(tasks)
    .then(function() {
        console.log('Build complete');
    })
    .catch(function(err) {
        console.error('Build error');
        console.error(err);
    });
