AuthSamples.StaticFilesAuth
=================

Sample demonstrating restricting access to static files using Authentication and Authorization. There are two different approaches.
Links to each scenario are provided on the home page.

1. For a given url path, allow only authenticated users to access static files. See /MapAuthenticatedFiles in Startup.cs.
1. For a given url path, use an authorization policy to determine who should have access to specific files. See /MapImperativeFile in startup.cs.

You can log in with any user name. For the policy scenario the user will only have access to the directory matching their name.