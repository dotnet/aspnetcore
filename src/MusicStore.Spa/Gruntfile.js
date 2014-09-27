/// <vs BeforeBuild='dev' Clean='clean' />

// node-debug (Resolve-Path ~\AppData\Roaming\npm\node_modules\grunt-cli\bin\grunt) task:target

module.exports = function (grunt) {

    grunt.initConfig({
        staticFilePattern: "**/*.{js,css,map,html,htm,ico,jpg,jpeg,png,gif,eot,svg,ttf,woff}",
        tslint: {
            options: {
                configuration: grunt.file.readJSON("grunt/tslint-rules.json")
            },
            files: {
                src: ["Client/**/*.ts", "!**/*.ng.ts"]
            }
        }
    });

    grunt.registerTask("ts", ["tslint", "tsng", "typescript:dev", "clean:tsng"]);
    grunt.registerTask("dev", ["clean", "copy", "less:dev", "ts"]);
    grunt.registerTask("release", ["clean", "copy", "uglify", "less:release", "typescript:release"]);
    grunt.registerTask("default", ["dev"]);

    require("grunt-ide-support")(grunt);
};