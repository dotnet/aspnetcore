export interface LinkList {
    title: string;
    entries: string[];
}

export const linkLists: LinkList[] = [{
    title: "Application uses",
    entries: [
        "Sample pages using ASP.NET MVC 6",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkId=518007\">Gulp</a> and <a href=\"http://go.microsoft.com/fwlink/?LinkId=518004\">Bower</a> for managing client-side libraries",
        "Theming using <a href=\"http://go.microsoft.com/fwlink/?LinkID=398939\">Bootstrap</a>"
    ]
}, {
    title: "How to",
    entries: [
        "<a href=\"http://go.microsoft.com/fwlink/?LinkID=398600\">Add a Controller and View</a>",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkID=699314\">Add an appsetting in config and access it in app.</a>",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkId=699315\">Manage User Secrets using Secret Manager.</a>",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkId=699316\">Use logging to log a message.</a>",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkId=699317\">Add packages using NuGet.</a>",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkId=699318\">Add client packages using Bower.</a>",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkId=699319\">Target development, staging or production environment.</a>"
    ]
}, {
    title: "Overview",
    entries: [
        "<a href=\"http://go.microsoft.com/fwlink/?LinkId=518008\">Conceptual overview of what is ASP.NET 5</a>",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkId=699320\">Fundamentals of ASP.NET 5 such as Startup and middleware.</a>",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkId=398602\">Working with Data</a>",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkId=398603\">Security</a>",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkID=699321\">Client side development</a>",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkID=699322\">Develop on different platforms</a>",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkID=699323\">Read more on the documentation site</a>"
    ]
}, {
    title: "Run & Deploy",
    entries: [
        "<a href=\"http://go.microsoft.com/fwlink/?LinkID=517851\">Run your app</a>",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkID=517852\">Run your app on .NET Core</a>",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkID=517853\">Run commands in your project.json</a>",
        "<a href=\"http://go.microsoft.com/fwlink/?LinkID=398609\">Publish to Microsoft Azure Web Apps</a>"
    ]
}];
