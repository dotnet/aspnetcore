AuthSamples.PathSchemeSelection
=================

Sample demonstrating selecting between cookie and another authentication scheme based on the request:

Cookie flow is the same, but notice in Startup how requests that start with /api are forwarded to the api scheme.
1. Run the app and click on the MyClaims tab, this should trigger a redirect to login.
2. Login with any username and password, the sample just validates that any values are provided.
3. You should be redirected back to /Home/MyClaims which will output a few user claims from the cookie
4. Now try going to /api/Home/MyClaims which will output a different set of claims (from the Api scheme)

Startup.cs is the most interesting class in the sample.
