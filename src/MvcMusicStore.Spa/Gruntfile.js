/// <reference path="node_modules/grunt/lib/grunt.js" />

// node-debug (Resolve-Path ~\AppData\Roaming\npm\node_modules\grunt-cli\bin\grunt) task:target

module.exports = function (grunt) {
    /// <param name="grunt" type="grunt" />
    
    grunt.loadNpmTasks('grunt-contrib-uglify');
    grunt.loadNpmTasks('grunt-contrib-watch');
    grunt.loadNpmTasks('grunt-contrib-copy');
    grunt.loadNpmTasks('grunt-contrib-clean');
    grunt.loadNpmTasks('grunt-contrib-less');
    grunt.loadNpmTasks('grunt-typescript');
    grunt.loadNpmTasks('grunt-tslint');
    grunt.loadNpmTasks('grunt-tsng');
    //grunt.loadNpmTasks('grunt-contrib-jshint');
    //grunt.loadNpmTasks('grunt-contrib-qunit');
    //grunt.loadNpmTasks('grunt-contrib-concat');

    grunt.initConfig({
        staticFilePattern: '**/*.{js,css,map,html,htm,ico,jpg,jpeg,png,gif,eot,svg,ttf,woff}',
        pkg: grunt.file.readJSON('package.json'),
        uglify: {
            options: {
                banner: '/*! <%= pkg.name %> <%= grunt.template.today("dd-mm-yyyy") %> */\n'
            },
            release: {
                files: {
                    'public/app.min.js': ['<%= typescript.dev.dest %>']
                }
            }
        },
        clean: {
            options: { force: true },
            bower: ['public'],
            assets: ['public'],
            tsng: ['client/**/*.ng.ts']
        },
        copy: {
            // This is to work around an issue with the dt-angular bower package https://github.com/dt-bower/dt-angular/issues/4
            fix: {
                files: {
                    "bower_components/jquery/jquery.d.ts": ["bower_components/dt-jquery/jquery.d.ts"]
                }
            },
            bower: {
                files: [
                    {   // JavaScript
                        expand: true,
                        flatten: true,
                        cwd: "bower_components/",
                        src: [
                            "modernizr/modernizr.js",
                            "jquery/dist/*.{js,map}",
                            "jquery.validation/jquery.validate.js",
                            "jquery.validation/additional-methods.js",
                            "bootstrap/dist/**/*.js",
                            "respond/dest/**/*.js",
                            "angular/*.{js,.js.map}",
                            "angular-route/*.{js,.js.map}",
                            "angular-bootstrap/ui-bootstrap*"
                        ],
                        dest: "public/js/",
                        options: { force: true }
                    },
                    {   // CSS
                        expand: true,
                        flatten: true,
                        cwd: "bower_components/",
                        src: [
                            "bootstrap/dist/**/*.css",
                        ],
                        dest: "public/css/",
                        options: { force: true }
                    },
                    {   // Fonts
                        expand: true,
                        flatten: true,
                        cwd: "bower_components/",
                        src: [
                            "bootstrap/**/*.{woff,svg,eot,ttf}",
                        ],
                        dest: "public/fonts/",
                        options: { force: true }
                    }
                ]
            },
            assets: {
                files: [
                    {
                        expand: true,
                        cwd: "Client/",
                        src: [
                            '<%= staticFilePattern %>'
                        ],
                        dest: "public/",
                        options: { force: true }
                    }
                ]
            }
        },
        less: {
            dev: {
                options: {
                    cleancss: false
                },
                files: {
                    "public/css/site.css": "Client/**/*.less"
                }
            },
            release: {
                options: {
                    cleancss: true
                },
                files: {
                    "public/css/site.css": "Client/**/*.less"
                }
            }
        },
        tsng: {
            options: {
                extension: ".ng.ts"
            },
            dev: {
                files: [
                    // TODO: Automate the generation of this config based on convention
                    {
                        src: ['Client/ng-apps/components/**/*.ts', 'Client/ng-apps/MusicStore.Store/**/*.ts', "!**/*.ng.ts"],
                        dest: "Client/ng-apps" // This needs to be the same across all sets so shared components work
                    },
                    {
                        src: ['Client/ng-apps/components/**/*.ts', 'Client/ng-apps/MusicStore.Admin/**/*.ts', "!**/*.ng.ts"],
                        dest: "Client/ng-apps" // This needs to be the same across all sets so shared components work
                    }
                ]
            }
        },
        tslint: {
            options: {
                configuration: grunt.file.readJSON("tslint.json")
            },
            files: {
                src: ['Client/**/*.ts', '!**/*.ng.ts']
            }
        },
        typescript: {
            options: {
                module: 'amd', // or commonjs
                target: 'es5', // or es3
                sourcemap: false
            },
            dev: {
                files: [
                    // TODO: Automate the generation of this config based on convention
                    {
                        src: ['Client/ng-apps/components/**/*.ng.ts', 'Client/ng-apps/MusicStore.Store/**/*.ng.ts'],
                        dest: 'public/js/MusicStore.Store.js'
                    },
                    {
                        src: ['Client/ng-apps/components/**/*.ng.ts', 'Client/ng-apps/MusicStore.Admin/**/*.ng.ts'],
                        dest: 'public/js/MusicStore.Admin.js'
                    }
                ]
            },
            release: {
                options: {
                    sourcemap: true
                },
                files: '<%= typescript.dev.files %>'
            }
        },
        watch: {
            typescript: {
                files: ['Client/**/*.ts', "!**/*.ng.ts"],
                tasks: ['ts']
            },
            dev: {
                files: ['bower_components/<%= staticFilePattern %>', 'Client/<%= staticFilePattern %>'],
                tasks: ['dev']
            }
        }
    });

    //grunt.registerTask('test', ['jshint', 'qunit']);
    grunt.registerTask('ts', ['tslint', 'tsng', 'typescript:dev']);
    grunt.registerTask('dev', ['clean', 'copy', 'less:dev', 'ts']);
    grunt.registerTask('release', ['clean', 'copy', 'uglify', 'less:release', 'typescript:release']);
    grunt.registerTask('default', ['dev']);
};