export interface CarouselItem {
    imageUrl: string;
    imageAlt: string;
    text: string;
    learnMoreUrl: string;
}

export const carouselItems: CarouselItem[] = [{
    imageUrl: "/images/ASP-NET-Banners-01.png",
    imageAlt: "ASP.NET",
    text: "Learn how to build ASP.NET apps that can run anywhere.",
    learnMoreUrl: "http://go.microsoft.com/fwlink/?LinkID=525028&clcid=0x409"
}, {
    imageUrl: "/images/Banner-02-VS.png",
    imageAlt: "Visual Studio",
    text: "There are powerful new features in Visual Studio for building modern web apps.",
    learnMoreUrl: "http://go.microsoft.com/fwlink/?LinkID=525030&clcid=0x409"
}, {
    imageUrl: "/images/ASP-NET-Banners-02.png",
    imageAlt: "Package Management",
    text: "Bring in libraries from NuGet, Bower, and npm, and automate tasks using Grunt or Gulp.",
    learnMoreUrl: "http://go.microsoft.com/fwlink/?LinkID=525029&clcid=0x409"
}, {
    imageUrl: "/images/Banner-01-Azure.png",
    imageAlt: "Microsoft Azure",
    text: "Learn how Microsoft's Azure cloud platform allows you to build, deploy, and scale web apps.",
    learnMoreUrl: "http://go.microsoft.com/fwlink/?LinkID=525027&clcid=0x409"
}];
