# Repo Dependencies

To support building and testing all the projects in the repo, various dependencies need to be installed. Some dependencies are required regardless of what project area you want to work in. Other dependencies are optional depending on the project 

## Required Dependencies

| Dependency                                                   | Use                                                          |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| [Git source control](https://git-scm.org/)                   | Used for cloning, branching, and other source control-related activities in the repo. |
| [.NET](https://dotnet.microsoft.com/)                        | A preview version of the .NET SDK is used for building projects within the repo. |
| [NodeJS]()                                                   | Used for building JavaScript assets in the repo.             |
| [Yarn](https://yarnpkg.com/)                                 | Used for installing JavaScript dependencies in the repo.     |
| [curl](https://curl.haxx.se/)/[wget](https://www.gnu.org/software/wget) | Used for downloading installation files and assets from the web. |
| [tar](http://gnuwin32.sourceforge.net/packages/gtar.htm)     | Used for unzipping installation assets. Included by default on macOS, Linux, and Windows 10 and above. |

## Optional Dependencies

| Dependency                                                   | Use                                                          |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| [Selenium](https://www.selenium.dev/)                        | Used to run integration tests in the `Components` (aka Blazor) project. |
| [Playwright](https://playwright.dev/)                        | Used to run template tests defined in the `ProjectTemplates` work. |
| [Chrome](https://www.google.com/chrome/)                     | Required when running tests with Selenium or Playwright in the projects above. When using Playwright, the dependency is automatically installed. |
| [Java Development Kit (v11 or newer)](https://jdk.java.net/) | Required when building the Java SignalR client. Can be installed using the `./eng/scripts/InstallJdk.ps1` script on Windows. |
| [Wix](https://wixtoolset.org/releases/)                      | Required when working with the Windows installers in the [Windows installers project](https://github.com/dotnet/aspnetcore/tree/main/src/Installers/Windows). |

