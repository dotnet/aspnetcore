## A pattern for making cross-repo breaking changes

The engineering team has come up with a pattern for making cross-repo breaking changes without destabilizing local \ CI builds using feature branches. I’ll explain it in terms of a breaking change in Configuration that affects Options:

1) Start by making a feature branch in the Configuration repo. A feature branch is any branch that starts with the prefix "feature/" e.g. feature/bring-back-web-config

`git checkout feature/bring-back-web-config`

2) Make your changes in this feature branch and push it to GitHub. You can ordinarily continue using this branch to get your PR reviewed.
3) TeamCity's individual Project configuration (http://aspnetci/project.html?projectId=Lite&tab=projectOverview) has always built feature branches. We've enabled an additional step to it that pushes packages produced from feature branches to https://dotnet.myget.org/f/aspnetcore-dev. 
Packages produced from feature branches will have a branch name suffix in their release label to distinguish them from our regular builds. For instance, a package produced for the branch pushed earlier might look like '2.1.0-preview2-bring-back-web-config-10012'.

4) In the Options repo, create a working branch like you normally do:

`git checkout prkrishn/react-to-config`

5) Once again in the options repo, edit build/dependencies.props to reference the feature branch package that got produced. 
a) If `build/dependencies.props` already has a reference to Configuration, update the version of the Options package in `build/dependencies.props` to point to the package produced from the feature branch.
b) If `build/dependencies.props` does not have a reference to the package version of Configuration, i.e. the package is transitively referenced:
    * Add a new entry in `build/dependencies.props` 
    * And a PackageReference to the feature branch package in your project.

```xml
// build/dependencies.props
<MicrosoftAspNetCoreConfigurationAbstractionsPackageVersion>2.1.0-preview2-bring-back-web-config-10012</MicrosoftAspNetCoreConfigurationAbstractionsPackageVersion>
```

5) Now that you reference the package with breaking changes, make your fixup changes to Options.
6) Get your code reviewed 
7) Check in to dev both sets of changes i.e. the feature branch from Configuration and your reaction changes in Options including changes to build/dependencies.props.
7) File a tracking task in Options to clean up build/dependencies.props. Build automation should fix up this version for you when it does it weekly update of dependencies.props, but it's good to manually verify that this is fixed up before we branch or create tags.

**tl,dr**: Push feature branches. TeamCity will build packages with release labels derived from branch name. You can edit and check in changes to build/dependencies.props to reference these packages.
