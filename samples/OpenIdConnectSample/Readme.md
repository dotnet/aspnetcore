# How to set up the sample locally

The OpenIdConnect sample supports multilpe authentication providers. In these instruction, we will explore how to set up this sample with both Azure Active Directory and Google Identity Platform.

## Determine your development environment and a few key variables

This sample is configured to run on port __44318__ locally. In Visual Studio, the setting is carried out in `.\properties\launchSettings.json`. When the application is run from command line, the URL is coded in `Program.cs`.

If the application is run from command line or terminal, environment variable ASPNETCORE_ENVIRONMENT should be set to DEVELOPMENT to enable user secret.

## Configure the Authorization server

### Configure with Azure Active Directory

1. Set up a new Azure Active Directory (AAD) in your Azure Subscription.
2. Open the newly created AAD in Azure web portal.
3. Navigate to the Applications tab.
4. Add a new Application to the AAD. Set the "Sign-on URL" to sample application's URL.
5. Naigate to the Application, and click the Configure tab.
6. Find and save the "Client Id".
7. Add a new key in the "Keys" section. Save value of the key, which is the "Client Secret".
8. Click the "View Endpoints" on the drawer, a dialog will shows six endpoint URLs. Copy the "OAuth 2.0 Authorization Endpoint" to a text editor and remove the "/oauth2/authorize" from the string. The remaining part is the __authority URL__. It looks like `https://login.microsoftonline.com/<guid>`.

### Configure with Google Identity Platform 

1. Create a new project through [Google APIs](https://console.developers.google.com).
2. In the sidebar choose "Credentials".
3. Navigate to "OAuth consent screen" tab, fill in the project name and save.
4. Navigate to "Credentials" tab. Click "Create credentials". Choose "OAuth client ID". 
5. Select "Web application" as the application type. Fill in the "Authorized redirect URIs" with `https://localhost:44318/signin-oidc`.
6. Save the "Client ID" and "Client Secret" shown in the dialog.
7. The "Authority URL" for Google Authentication is `https://accounts.google.com/`.

## Configure the sample application

1. Restore the application.
2. Set user secrets:

 ```
dotnet user-secrets set oidc:clientid <Client Id>
dotnet user-secrets set oidc:clientsecret <Client Secret>
dotnet user-secrets set oidc:authority <Authority URL>
```

