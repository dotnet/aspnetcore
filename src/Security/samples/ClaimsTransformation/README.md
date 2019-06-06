AuthSamples.ClaimsTransformation
=================

Sample demonstrating claims transformation:
1. Run the app and click on the MyClaims tab, this should trigger a redirect to login.
2. Login with any username and password, the sample just validates that any values are provided.
3. You should be redirected back to /Home/MyClaims which will show a few user claims including a Transformed time.
4. Refresh the page and see that the Transformed time updates.

Startup.cs and ClaimsTransformer.cs are the most interesting classes in this sample.
