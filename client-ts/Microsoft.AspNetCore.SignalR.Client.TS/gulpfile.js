const gulp       = require('gulp');
const browserify = require('browserify');
const ts         = require('gulp-typescript');
const source     = require('vinyl-source-stream');
const buffer     = require('vinyl-buffer');
const del        = require('del');
const rename     = require('gulp-rename');
const babel      = require('gulp-babel');

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

function browserifyModule(sourceFileName, namespace, targetFileName) {
    const browserOutDir = clientOutDir + '/../browser';

    return browserify(clientOutDir + '/' + sourceFileName, {standalone: namespace})
        .bundle()
        .pipe(source(targetFileName))
        .pipe(gulp.dest(browserOutDir))
        .pipe(buffer())
        .pipe(rename({ extname: '.min.js' }))
        .pipe(babel({presets: ['minify']}))
        .pipe(gulp.dest(browserOutDir));
}

gulp.task('browserify-client', ['compile-ts-client'], () => {
    return browserifyModule('HubConnection.js', 'signalR', 'signalr-client.js');
});

gulp.task('browserify-msgpackprotocol', ['compile-ts-client'], () => {
    return browserifyModule('MessagePackHubProtocol.js', 'signalRMsgPack', 'signalr-msgpackprotocol.js');
});

gulp.task('browserify', [ 'browserify-client', 'browserify-msgpackprotocol']);

gulp.task('build-ts-client', ['clean', 'compile-ts-client', 'browserify']);

gulp.task('default', ['build-ts-client']);
