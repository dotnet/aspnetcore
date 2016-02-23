import * as React from 'react';

export interface LinkList {
    title: string;
    entries: JSX.Element[];
}

export const linkLists: LinkList[] = [{
    title: "Application uses",
    entries: [
        <div>Sample pages using ASP.NET MVC 6</div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkId=518007">Gulp</a> and <a href="http://go.microsoft.com/fwlink/?LinkId=518004">Bower</a> for managing client-side libraries</div>,
        <div>Theming using <a href="http://go.microsoft.com/fwlink/?LinkID=398939">Bootstrap</a></div>
    ]
}, {
    title: "How to",
    entries: [
        <div><a href="http://go.microsoft.com/fwlink/?LinkID=398600">Add a Controller and View</a></div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkID=699314">Add an appsetting in config and access it in app.</a></div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkId=699315">Manage User Secrets using Secret Manager.</a></div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkId=699316">Use logging to log a message.</a></div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkId=699317">Add packages using NuGet.</a></div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkId=699318">Add client packages using Bower.</a></div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkId=699319">Target development, staging or production environment.</a></div>
    ]
}, {
    title: "Overview",
    entries: [
        <div><a href="http://go.microsoft.com/fwlink/?LinkId=518008">Conceptual overview of what is ASP.NET 5</a></div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkId=699320">Fundamentals of ASP.NET 5 such as Startup and middleware.</a></div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkId=398602">Working with Data</a></div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkId=398603">Security</a></div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkID=699321">Client side development</a></div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkID=699322">Develop on different platforms</a></div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkID=699323">Read more on the documentation site</a></div>
    ]
}, {
    title: "Run & Deploy",
    entries: [
        <div><a href="http://go.microsoft.com/fwlink/?LinkID=517851">Run your app</a></div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkID=517852">Run your app on .NET Core</a></div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkID=517853">Run commands in your project.json</a></div>,
        <div><a href="http://go.microsoft.com/fwlink/?LinkID=398609">Publish to Microsoft Azure Web Apps</a></div>
    ]
}];
