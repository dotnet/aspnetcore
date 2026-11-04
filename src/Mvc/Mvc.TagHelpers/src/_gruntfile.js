// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

module.exports = function (grunt) {
    grunt.initConfig({
        jshint: {
            scripts: [ "js/**/*.js" ]
        },
        uglify: {
            scripts: {
                files: [{
                    expand: true,
                    cwd: "js",
                    src: "**/*.js",
                    dest: "compiler/resources"
                }]
            }
        }
    });

    grunt.loadNpmTasks("grunt-contrib-jshint");
    grunt.loadNpmTasks("grunt-contrib-uglify");

    grunt.registerTask("default", [ "jshint", "uglify" ]);
};