We merged together prototypes of three validation-related features: localization of validation messages in M.E.V, async validation support in Blazor forms, and client-side validation without interactivity in Blazor SSR. Lets create a few demo apps that will showcase them.



In ./src/Validation/demos, create a new solution ValidationDemos.sln and in it three separate apps:



1\) Blazor Server app

&#x20;   - Simple good looking app with a form page

&#x20;   - Global server interactivity, no Wasm

&#x20;   - Form has model with standard sync validation attributes abd some async validation attributes like UniqueUsernameAttribute (derived from the AsyncValidationAttribute we added in this branch)

&#x20;   - The form should have nice pending validation indicators and faulted validation indicators

&#x20;   - Attributes have error messages specified (via ErrorMessage property), backed by JSON files (support English and Spanish)

&#x20;   - To localize via JSON files, create a simple custom IStringLocalizer implementation in the app that reads a JSON file which has structure { "en": { "someKey": "some en value", ... }, ... } the needed languages and message keys. Register that localizer (factory) into DI in the app.

&#x20;   - All the UI texts should also be localized via the IStringLocalizer (so that the demo UI has consistent localized experience)

&#x20;   - It should be possible to switch the language via language picker (it changes the UI and the validation messages), changing language should do a hard reload of the page with the new language

&#x20;   - The goal is to show a) the async features and UX, b) validation localization, including supporting non-resource file sources

&#x20;

2\) Blazor SSR app

&#x20;   - Simple good looking app with a form page

&#x20;   - No interactivity, just SSR components

&#x20;   - No async validation features    

&#x20;   - Form uses enhanced mode

&#x20;   - Form has model with standard sync validation attributes that support client-side validation

&#x20;   - The SSR app needs builder.Services.AddClientSideValidation() and <ClientSideValidator /> in the form

&#x20;   - Attributes have error messages specified (via ErrorMessage property), backed by resource files (support English and Spanish)

&#x20;   - The goal is to show that SSR generates the proper data- attributes for client-side validation (including localized messages) and that the JS library then properly validates the rules before letting the form submit to the server 



3\) MVC app

&#x20;   - Simple good looking app with a form page

&#x20;   - No localization needed here, no async validation (it is not supported in MVC at all)

&#x20;   - Form has model with standard sync validation attributes that support client-side validation

&#x20;   - The app should include aspnet-core-validation.js via <script> tag and remove the jQuery validation scripts (or similar mechanism that ensures the new JS library is used)

&#x20;   - The goal is to show that existing MVC app with client validation can use the new JS validation library as drop-in replacement for the jQuery-based library, i.e. MVC generates data- attributes exactly the same as before (using the existing MVC pipeline) and the new JS library picks these up and validates the rules before letting the form submit to the server



Make sure it is easily visible which app is which (e.g., put a logo on top, use different color palette - blue for server, green for SSR, orange for MVC).



See the project file for ./src/Components/Samples/BlazorUnitedApp on how to make a sample app work in the aspnetcore repo (you need a lot of explicit references, just copy them all).



The SSR and MVC app need the aspnet-core-validation.js file. Build the Web.JS project and copy it to their wwwroot/js manually

