/// <binding Clean='clean' />
"use strict";

var gulp = require("gulp"),
    bowerFiles = require('main-bower-files');

var paths = {
    webroot: "./wwwroot/"
};

paths.bowerFilesDest = paths.webroot + '/bower_components';

gulp.task("copy:bower", function () {
    return gulp.src(bowerFiles()).pipe(gulp.dest(paths.bowerFilesDest));
});

gulp.task("default", ["copy:bower"]);

