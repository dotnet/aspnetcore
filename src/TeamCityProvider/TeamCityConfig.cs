// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace TeamCityApi
{
    public class TeamCityConfig
    {
        public string Server
        {
            get
            {
                return "aspnetci";
            }
        }

        public string User
        {
            get
            {
                return "redmond\\asplab";
            }
        }

        public string Password { get; set; }
    }
}
