TagHelperSample.Web
===
This sample web project illustrates TagHelper use. Please build from root
(`.\build.cmd` on Windows; `./build.sh` elsewhere) before using this site.

### What this sample contains
1. Creating, editing and viewing custom users with validation.
  1. `/` lists current users.
  2. `/Home/Create` create a new user.
  3. `/Home/Edit/{id:int}` edit a user.
2. View Components and nested caching in views.
  1. `/Movies/` Current movie ratings and critic quotes. Click on the buttons on the page to update values in the side bar.
3. Custom TagHelper to enable browser detection based content.
  1. `/TagHelper/ConditionalComment` Shows different content if your browser is IE7.
