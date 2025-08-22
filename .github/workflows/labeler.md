# Issue-Labeler Workflows

This repository uses actions from [dotnet/issue-labeler](https://github.com/dotnet/issue-labeler) to predict area labels for issues and pull requests.

The following workflow templates were imported and updated from [dotnet/issue-labeler/wiki/Onboarding](https://github.com/dotnet/issue-labeler/wiki/Onboarding):

1. `labeler-cache-retention.yml`
2. `labeler-predict-issues.yml`
3. `labeler-predict-pulls.yml`
4. `labeler-promote.yml`
5. `labeler-train.yml`

## Repository Configuration

Across these workflows, the following changes were made to configure the issue labeler for this repository:

1. Set `LABEL_PREFIX` to `"area-"`:
    - `labeler-predict-issues.yml`
    - `labeler-predict-pulls.yml`
    - `labeler-train.yml`
2. Set `DEFAULT_LABEL` to `"needs-area-label"`:
    - `labeler-predict-issues.yml`
    - `labeler-predict-pulls.yml`
3. Remove the `EXCLUDED_AUTHORS` value as we do not bypass labeling for any authors' issues/pulls in this repository:
    - `labeler-predict-issues.yml`
    - `labeler-predict-pulls.yml`
4. Update the pull request labeling branches to include `main` and `release/*`:
    - `labeler-predict-pulls.yml`
5. Remove the `repository` input for training the models against another repository:
    - `labeler-train.yml`
6. Update the cache retention cron schedule to an arbitrary time of day:
    - `labeler-cache-retention.yml`
