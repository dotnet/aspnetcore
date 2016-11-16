const gulp = require('gulp');
const browserify = require('browserify');
const ts = require('gulp-typescript');
const source = require('vinyl-source-stream');
const del = require('del');
const argv = require('yargs').argv;

const tsProject = ts.createProject('./tsconfig.json');
const clientOutDir = tsProject.options.outDir;

gulp.task('clean', () => {
    return del([clientOutDir + '/..'], { force: true });
});

gulp.task('compile-ts-client', () => {
    return tsProject.src()
        .pipe(tsProject())
        .pipe(gulp.dest(clientOutDir));
});

gulp.task('browserify-client', ['compile-ts-client'], () => {
    return browserify(clientOutDir + '/RpcConnection.js', {standalone: 'signalR'})
        .bundle()
        .pipe(source('signalr-client.js'))
        .pipe(gulp.dest(clientOutDir + '/../signalr-client-bundle'));
});

gulp.task('build-ts-client', ['clean', 'compile-ts-client', 'browserify-client']);

gulp.task('bundle-client', ['build-ts-client'], () => {
    if (!argv.bundleOutDir) {
        console.log('Use \'--bundleOutDir\' option to specify the target file for the bundled client.');
    }
    else {
        return gulp.src(clientOutDir + '/../signalr-client-bundle/signalr-client.js')
            .pipe(gulp.dest(argv.bundleOutDir));
    }
});

gulp.task('default', ['build-ts-client']);