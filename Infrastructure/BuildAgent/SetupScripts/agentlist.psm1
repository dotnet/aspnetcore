function Get-Agents {
    return @(
        # macos
        @{
            Name      = 'aspnet-mac-a02'
            OS        = 'macOS'
            OSVersion = '10.12'
        },
        @{
            Name      = 'aspnet-mac-a03'
            OS        = 'macOS'
            OSVersion = '10.12'
        },
        @{
            Name      = 'aspnetci-mac'
            OS        = 'macOS'
            OSVersion = '10.12'
        },
        # codesign
        @{
            Name     = 'asp-sign-01'
            OS       = 'Windows'
            Category = 'Codesign'
        },
        @{
            Name     = 'asp-sign-02'
            OS       = 'Windows'
            Category = 'Codesign'
        },
        # ubuntu
        @{
            Name      = 'aspnetci-a-104'
            OS        = 'Linux'
            OSVersion = 'Ubuntu14'
        },
        @{
            Name      = 'asp-ubuntu1604-01'
            OS        = 'Linux'
            OSVersion = 'Ubuntu16'
        },
        @{
            Name      = 'aspnetci-a-111'
            OS        = 'Linux'
            OSVersion = 'Ubuntu16'
        },
        @{
            Name      = 'asp-ubuntu1804-01'
            OS        = 'Linux'
            OSVersion = 'Ubuntu18'
        },
        @{
            Name      = 'asp-ubuntu1804-02'
            OS        = 'Linux'
            OSVersion = 'Ubuntu18'
        },
        # Windows
        @{
            Name      = 'aspnetci-a-100'
            OS        = 'Windows'
            OSVersion = 'Server2012'
        },
        @{
            Name      = 'aspnetci-a-101'
            OS        = 'Windows'
            OSVersion = 'Server2012'
        },
        @{
            Name      = 'aspnetci-a-102'
            OS        = 'Windows'
            OSVersion = 'Server2012'
        },
        @{
            Name      = 'aspnetci-a-103'
            OS        = 'Windows'
            OSVersion = 'Server2012'
        },
        @{
            Name      = 'aspnetci-a-106'
            OS        = 'Windows'
            OSVersion = 'Server2012'
        },
        @{
            Name      = 'aspnetci-a-107'
            OS        = 'Windows'
            OSVersion = 'Server2012'
        },
        @{
            Name      = 'aspnetci-a-108'
            OS        = 'Windows'
            OSVersion = 'Server2012'
        },
        @{
            Name      = 'aspnetci-a-116'
            OS        = 'Windows'
            OSVersion = 'Server2012'
        },
        @{
            Name      = 'aspnetci-a-109'
            OS        = 'Windows'
            OSVersion = 'Server2012'
        },
        # 
        @{
            Name      = 'aspnetci-a-110'
            OS        = 'Windows'
            OSVersion = 'Server2008'
        },
        @{
            Name      = 'aspnetci-a-112'
            OS        = 'Windows'
            OSVersion = 'Win7'
        },
        @{
            Name      = 'aspnetci-a-113'
            OS        = 'Windows'
            OSVersion = 'Win8'
        },
        @{
            Name      = 'aspnetci-a-114'
            OS        = 'Windows'
            OSVersion = 'Win10'
        }
    )
}
