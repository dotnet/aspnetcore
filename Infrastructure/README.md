## RAAS

RAAS automatically triages build/test failures and creates/updates issues in GitHub accordingly.

## Running locally

- Create a personal access token in GitHub (https://github.com/settings/tokens)
- Create a personal access token in VSTS (https://dev.azure.com/dnceng/_usersSettings/tokens)
- Run the following command

```
dotnet run --github-access-token <github_token> --team-city-username <username> --team-city-password <pass> --smtp-login <email> --smtp-password <pass> --vsts-pat <vsts_token>
```

Note:
Running locally will still create/comment on issues on GitHub. Make sure to comment out those lines before running if this isn't intended.