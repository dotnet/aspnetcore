/// <binding AfterBuild='build' Clean='clean' />

"use strict";

var path = require('path');
var gulp = require('gulp');
var del = require('del');
var typescript = require('gulp-typescript');
var inlineNg2Template = require('gulp-inline-ng2-template');
var sourcemaps = require('gulp-sourcemaps');

var webroot = "./wwwroot/";

var config = {
    libBase: 'node_modules',
    lib: [
        require.resolve('bootstrap/dist/css/bootstrap.css'),
        path.dirname(require.resolve('bootstrap/dist/fonts/glyphicons-halflings-regular.woff')) + '/**',
        require.resolve('angular2/bundles/angular2-polyfills.js'),
        require.resolve('traceur/bin/traceur-runtime.js'),
        require.resolve('es6-module-loader/dist/es6-module-loader-sans-promises.js'),
        require.resolve('systemjs/dist/system.src.js'),
        require.resolve('angular2/bundles/angular2.dev.js'),
        require.resolve('angular2/bundles/router.dev.js'),
        require.resolve('angular2/bundles/http.dev.js'),
        require.resolve('angular2-aspnet/bundles/angular2-aspnet.js'),
        require.resolve('jquery/dist/jquery.js'),
        require.resolve('bootstrap/dist/js/bootstrap.js'),
        require.resolve('rxjs/bundles/Rx.js')
    ]
};

gulp.task('build.lib', function () {
    return gulp.src(config.lib, { base: config.libBase })
        .pipe(gulp.dest(webroot + 'lib'));
});

gulp.task('build', ['build.lib'], function () {
    var tsProject = typescript.createProject('./tsconfig.json', { typescript: require('typescript') });
    var tsSrcInlined = gulp.src([webroot + '**/*.ts', 'typings/**/*.d.ts'], { base: webroot })
        .pipe(inlineNg2Template({ base: webroot }));
    return tsSrcInlined
        .pipe(sourcemaps.init())
        .pipe(typescript(tsProject))
        .pipe(sourcemaps.write())
        .pipe(gulp.dest(webroot));
});

gulp.task('clean', function () {
    return del([webroot + 'lib']);
});

gulp.task('default', ['build']);
