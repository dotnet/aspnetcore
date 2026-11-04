# Generating template-baselines.json

For small project template changes, you may be able to edit the `template-baselines.json` file manually. This is a good way to ensure you have correct expectations about the effects of your changes.

For larger changes such as adding entirely new templates, it may be impractical to type out the changes to `template-baselines.json` manually. In those cases you can follow a procedure like the following.

  1. Follow the [manual generated-application workflow](README.md#prepare-generated-application-validation).
  2. Run the appropriate `scripts\Run-*-Locally.ps1` script. The script repacks and reinstalls the current template
     package before recreating the generated project, so it can be rerun after each source edit.
  3. After generating a particular project's output, the following can be run in a Bash prompt (e.g., using WSL):
        - `cd src/ProjectTemplates/scripts`
        - `export PROJECT_NAME=MyBlazorApp` (update as necessary; this is the directory directly under `scripts` that contains your project output)
        - `find $PROJECT_NAME -type f -not -path "*/obj/*" -not -path "*/bin/*" -not -path "*/.publish/*" | sed -e "s/^$PROJECT_NAME\///" | sed -e "s/$PROJECT_NAME/{ProjectName}/g" | sed 's/.*/        "&",/' | sort -f`
        - This will emit the JSON-formatted lines you can manually insert into the relevant place inside `template-baselines.json`
