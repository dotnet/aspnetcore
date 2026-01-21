Preparing new servicing updates
===============================

In order to prepare this repo to build a new servicing update, the following changes need to be made.

* Increment the patch version in the [eng/Versions.props](/eng/Versions.props) file in the repository root.

    ```diff
    -  <AspNetCorePatchVersion>7</AspNetCorePatchVersion>
    +  <AspNetCorePatchVersion>8</AspNetCorePatchVersion>
    ```
