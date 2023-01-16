// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using BestForYouRecipes;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<IRecipesStore, RecipesStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles(); // Enable WebAssembly
app.UseStaticFiles();

app.UseRouting();

app.MapRazorComponents();
app.MapBlazorHub();

app.Map("images/uploaded/{filename}", async (string filename, IRecipesStore recipeStore) =>
    Results.File(await recipeStore.GetImage(filename), "image/jpeg"));

app.MapPost("api/recipes", async (Recipe recipe, IRecipesStore recipeStore) =>
    await recipeStore.AddRecipe(recipe)); // TODO: Validate

app.MapPost("api/images", async (HttpRequest req, IRecipesStore recipeStore) =>
    await recipeStore.AddImage(req.Body));

app.Run();
