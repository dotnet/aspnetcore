/* Release checklist
- Run `git changelog` and edit to match previous output (this should make use of jquey-release instead)
- pull latest https://github.com/jquery/jquery-release
- run
	node release.js --remote=jzaefferer/jquery-validation
- Wait a while, verify and confirm each step
-
*/

/*jshint node:true */
module.exports = function( Release ) {

function today() {
	return new Date().toISOString().replace(/T.+/, "");
}

// also add version property to this
Release._jsonFiles.push( "validation.jquery.json" );

Release.define({
	issueTracker: "github",
	changelogShell: function() {
		return Release.newVersion + " / " + today() + "\n==================\n\n";
	},

	generateArtifacts: function( done ) {
		Release.exec( "grunt release", "Grunt command failed" );
		done([
			"dist/additional-methods.js",
			"dist/additional-methods.min.js",
			"dist/jquery.validate.js",
			"dist/jquery.validate.min.js"
		]);
	},

	// disable CDN publishing
	cdnPublish: false,

	// disable authors check
	_checkAuthorsTxt: function() {}
});

};
