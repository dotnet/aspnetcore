# Suggested demo sequence

Before people start watching:

 * In `SubmitRecipe.razor`:
   - Remove `@attribute [ComponentRenderMode(WebComponentRenderMode.Auto)]`
   - On the `<IngredientsListEditor>`, change `@bind-Ingredients="@recipe.Ingredients"` to just `Ingredients="@recipe.Ingredients"`
 * In `MainLayout.razor`, remove `<script src="_framework/blazor.united.js" suppress-error="BL9992"></script>`
 * In `BestForYouReceipts.csproj`, remove `net7.0-browser` from `TargetFrameworks`
 * In `StarRatingReviews.razor`, remove `enhance` from the `<EditForm>`
 * In `Index.razor`, remove `@attribute [StreamRendering(true)]`

 Actual demo:

 * Run app, see recipe list. Search for `choc` and view a recipe.
   - Explain: for this kind of app you want pure HTML rendering for public pages, not circuits or WebAssembly
 * **MAIN POINT 1: We can now use `.razor` for pure passive HTML rendering.**
   - See `Index.razor` is basic Blazor component. Same Blazor programming model (components, parameters, etc.).
   - See `Program.Server.cs` contains `app.MapRazorComponents()` (so no `_Host.cshtml` is needed)
   - See `MainLayout.razor` (no `App.razor` needed). Notice there are **no script tags** (no scripts are loaded).
   - We can also use it for traditional HTML form posts
     - See `StarRatingReviews.razor` - it's the `<EditForm>` and `<ValidationMessage>` you already know from Blazor
     - In app, submit a review. See that validation works (try leaving review fields blank).
     - Notice that we lose the scroll position on submit - will fix that in a minute
 * **MAIN POINT 2: We can progressively enhance things, without needing circuits or WebAssembly**
   - Show that `RecipeStore.Server.cs`'s `GetRecipes` is simulating a long-running DB query (delays for 1 second)
   - In `Index.razor`, add `@attribute [StreamRendering(true)]` and see the effect
     - It's the existing Blazor programming model for displaying a "loading" state
     - Also see it's working when you search (this is just another form post)
     - Explain how streaming rendering works
   - Now enhance navigation. In `MainLayout.razor`, add `<script src="_framework/blazor.united.js" suppress-error="BL9992"></script>`
     - See that navigation is now faster and doesn't have to reload the whole page
   - Now enhance form posts. In `StarRatingReviews.razor` add `enhance` to the `EditForm`
     - See that on submission, you no longer lose scroll position (it's not reloading the whole page any more)
 * **MAIN POINT 3: We can add interactive Blazor Server components or pages**
   - Go to the "Submit Recipe" page. Observe that you can't "Add" an ingredient yet (button does nothing)
   - In `SubmitRecipe.razor`, on `<IngredientsListEditor>`, add `rendermode="@WebComponentRenderMode.Server"`
     - See you can now add, switch between metric/imperial, drag-drop to reorder, etc.
     - Use browser dev tools to show the circuit was created on demand when you can to this page
     - (Not yet implemented) Circuits will also be removed on demand when you navigate away from a place that needs them
     - **Key observation**: Many sites don't need a circuit all the time. Pay as you go.
   - Now let's make this whole page interactive
     - On `SubmitRecipe.razor`, add `@attribute [ComponentRenderMode(WebComponentRenderMode.Server)]`
     - Also change the `<IngredientsListEditor>` to be `<IngredientsListEditor @bind-Ingredients="@recipe.Ingredients" />`
       (don't need any rendermode any more, as the whole page is now interactive)
     - Try submitting an empty form - see all the validation working.
     - Actually submit a recipe, including uploading a photo. Find it on the passively-rendered homepage at the bottom.
 * **MAIN POINT 4: We can use WebAssembly in the same project too**
   - In the `.csproj`, add `;net7.0-browser` to the `TargetFrameworks`. See it now builds for wasm too.
   - In `SubmitRecipe.razor`, change the page's render mode to `WebComponentRenderMode.WebAssembly`
     - See it still works. Use dev tools to show that the WebAssembly files were loaded only when you navigated to that page.
   - **Key observation**: You no longer have to choose up-front whether to use Server or WebAssembly. You can decide later,
     and even change it on a per-page or even per-component level.
     - We will later support using both Server and WebAssembly at the same time if you want, possibly even interleaving their
       components, but that's not in this prototype yet.
 * **MAIN POINT 5: For fast startup, we can auto-pick between Server and WebAssembly**
   - In `SubmitRecipe.razor`, change the page's render mode to `WebComponentRenderMode.Auto`
   - Use dev tools with cleared cache and reload to see it uses server, then reload to see it uses WebAssembly
   - **Key observation**: Server gives fastest startup, and we can download wasm files in background then use them next time
     so you don't have the cost of a circuit on the server.
     - You don't have to write any difficult state-transferring code to enable this. It's a natural effect of the user
       navigating around the site. Just try to keep state at the per-page level (not global).
