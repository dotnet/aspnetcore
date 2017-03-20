# How to set up the sample locally

## Set up [Azure Active Directory](https://azure.microsoft.com/en-us/documentation/services/active-directory/)

1. Create your own Azure Active Directory (AD). Save the "tenent name".
2. Add a new Application: in the Azure AD portal, select Application, and click Add in the drawer.
3. Set the sign-on url to `http://localhost:42023`.
4. Select the newly created Application, navigate to the Configure tab.
5. Find and save the "Client Id"
8. In the keys section add a new key. A key value will be generated. Save the value as "Client Secret"

## Configure the local environment
1. Set environment ASPNETCORE_ENVIRONMENT to DEVELOPMENT. ([Working with Multiple Environments](https://docs.asp.net/en/latest/fundamentals/environments.html))
2. Set up user secrets:
```
dotnet user-secrets set oidc:clientid <Client Id>
dotnet user-secrets set oidc:clientsecret <Client Secret>
dotnet user-secrets set oidc:authority https://login.windows.net/<Tenent Name>.onmicrosoft.com
```

