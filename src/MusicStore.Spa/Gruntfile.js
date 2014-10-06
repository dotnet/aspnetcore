/// <vs Clean='clean' />
// node-debug (Resolve-Path ~\AppData\Roaming\npm\node_modules\grunt-cli\bin\grunt) task:target

module.exports = function (grunt) {

	grunt.loadNpmTasks("grunt-bower-task");

    grunt.initConfig({
        staticFilePattern: "**/*.{js,css,map,html,htm,ico,jpg,jpeg,png,gif,eot,svg,ttf,woff}",
        bower: {
	        install: {
			    options: {
			        targetDir: "wwwroot/lib",
			        layout: "byComponent",
			        cleanTargetDir: true
			    }
			}
		}
    });

    grunt.registerTask("ts", ["tslint", "tsng", "typescript:dev", "clean:tsng"]);
    grunt.registerTask("dev", ["clean:assets", "copy", "bower:install", "less:dev", "ts"]);
    grunt.registerTask("release", ["clean", "copy", "uglify", "less:release", "typescript:release"]);
    grunt.registerTask("default", ["dev"]);

    require("grunt-ide-support")(grunt);
};