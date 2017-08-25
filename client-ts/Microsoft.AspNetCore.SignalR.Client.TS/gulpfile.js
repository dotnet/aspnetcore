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

function browserifyModuleES5(sourceFileName, namespace, targetFileName, hasAsync) {
    const browserOutDir = clientOutDir + '/../browser';

    let babelOptions = { presets: ['es2015'] };
    if (hasAsync) {
        babelOptions.plugins = ['transform-runtime'];
    }

    return browserify(clientOutDir + '/' + sourceFileName, {standalone: namespace})
        .transform('babelify', { presets: ['es2015'], plugins: ['transform-runtime'] })
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

gulp.task('browserify-clientES5', ['compile-ts-client'], () => {
    return browserifyModuleES5('HubConnection.js', 'signalR', 'signalr-clientES5.js', /*hasAsync*/ true);
});

gulp.task('browserify-msgpackprotocolES5', ['compile-ts-client'], () => {
    return browserifyModuleES5('MessagePackHubProtocol.js', 'signalRMsgPack', 'signalr-msgpackprotocolES5.js', /*hasAsync*/ false);
});

gulp.task('browserify', [ 'browserify-client', 'browserify-msgpackprotocol', 'browserify-clientES5', 'browserify-msgpackprotocolES5']);

gulp.task('build-ts-client', ['clean', 'compile-ts-client', 'browserify']);

gulp.task('default', ['build-ts-client']);
