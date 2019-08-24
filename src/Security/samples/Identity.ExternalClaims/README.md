AuthSamples.Identity.ExternalClaims
=================

Sample demonstrating copying over static and dynamic external claims from Google authentication during login:

Steps:
1. Configure a google OAuth2 project. See https://docs.microsoft.com/aspnet/core/security/authentication/social/google-logins for basic setup using google logins.
2. Update Startup.cs AddGoogle()'s options with ClientId and ClientSecret for your google app.
3. Run the app and click on the MyClaims tab, this should trigger a redirect to login.
4. Login via the Google button, this should redirect you to google.
3. You should be redirected back to /Home/MyClaims which will output the user claims. Notice that a gender claim is included as well as the AccessToken in the AuthenticationProperties.

How this works:
- Startup adds the google plus scope to include additional data in the json payload, and then maps the gender key to a claim in the google identity which is stored in the Identity.ExternalCookie.
- In ExternalLogin.cshtml.cs when a user is registered from an external login, we copy the gender claim into the user's local identity claims in OnPostConfirmationAsync, since this is a static claim we only do this once.
- For the AccessToken which needs to be updated every login, this is done automatically via SaveTokens = true, which calls StoreTokens on AuthenticationProperties which is passed into SignInAsync, this is done in both Register and Login so its updated every time an external login is done.
- To demonstrate adding custom tokens, see OnCreatingTicket which adds a new token and stores the updated tokens.
