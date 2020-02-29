AuthSamples.Cookies
=================

Sample demonstrating cookie authentication:
1. Run the app and click on the MyClaims tab, this should trigger a redirect to login.
2. Login with any username and password, the sample just validates that any values are provided.
3. You should be redirected back to /Home/MyClaims which will output a few user claims.

Startup.cs and Controllers/AccountController.cs are the most interesting classes in the sample.
