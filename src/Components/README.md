# Components library shared by MVC and Blazor

## Run end-to-end tests

Prerequisites:
- Install [selenium-standalone](https://www.npmjs.com/package/selenium-standalone) (requires Java 8 or 9)
  - [Open JDK9](http://jdk.java.net/java-se-ri/9)
  - `npm install -g selenium-standalone`
  - `selenium-standalone install`
- Chrome

Run `selenium-standalone start`

Run `build.cmd /t:Test` or `build.sh /t:Test`
