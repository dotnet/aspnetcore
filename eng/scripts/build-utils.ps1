# Walks up the source tree, starting at the given file's directory, and returns a FileInfo object for the first .csproj file it finds, if any.
# Initial implimentation taken from - dotnet/roslyn repository
function Get-ProjectFile([object]$fileInfo) {
    Push-Location

    Set-Location (Get-Item $fileInfo).Directory

    try {
        while ($true) {
            # search up from the current file for a folder containing a project file
            $files = Get-ChildItem -Path (Get-Location) -Filter "*.csproj"

            if ($files) {
                return ($files[0]).FullName
            }
            else {
                $location = Get-Location
                Set-Location ..
                if ((Get-Location).Path -eq $location.Path) {
                    # our location didn't change. We must be at the drive root, so give up
                    return $null
                }
            }
        }
    }
    finally {
        Pop-Location
    }
}
