const gulp = require('gulp');

gulp.task('copy-jasmine', () => {
  gulp.src(['../../node_modules/jasmine-core/lib/jasmine-core/*.js', '../../node_modules/jasmine-core/lib/jasmine-core/*.css'])
    .pipe(gulp.dest('./wwwroot/lib/jasmine/'));
});