// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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