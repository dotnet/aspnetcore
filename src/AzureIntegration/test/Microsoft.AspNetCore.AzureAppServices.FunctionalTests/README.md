Running functional tests locally:

1. Set following environment variables:
    -. `SiteExtensionFeed` - feed where site extension is published
    -. `APIKEY` - Nuget API key for extension publish feed
    -. `AZURE_AUTH_CLIENT_ID` - Azure service principal client id
    -. `AZURE_AUTH_CLIENT_SECRET` - Azure service principal client secret
    -. `AZURE_AUTH_TENANT` - Azure service principal tenant
    -. See https://github.com/Azure/azure-sdk-for-net/blob/Fluent/AUTH.md on how to create service principal

2. Run `.\build /t:BuildSiteExtension /t:PushSiteExtension` to build and push site extension
2. Run `.\build /t:Test /p:AntaresTests=true` to run tests using the site extension