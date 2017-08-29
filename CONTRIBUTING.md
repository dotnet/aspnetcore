Contributing
======

General Information on contributing is in the [Contributing Guide](https://github.com/aspnet/Home/blob/dev/CONTRIBUTING.md) in the Home repo.

Specific guidelines for this repo are as follows:
# Contributing to an existing template
## Updating preexisting content 
If you are simply editing a preexisting file, you can make the code change and submit a pull request.
## Adding or removing a file

**When updating a base template, the following also needs to be updated:**
* *.vstemplate located at src\BaseTemplates folder for the template being updated.

**When updating a template rule, the following also needs to be updated:**

* Templates.xml located at src\Templates.xml: Update the appropriate AddFile or ReplaceFile rule as needed. 

* TemplateRules.csproj: Update the appropriate LooseFiles element as needed.

# Contributing a new template
Please open an issue for new template requests.
