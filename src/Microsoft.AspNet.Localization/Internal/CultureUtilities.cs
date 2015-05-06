// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Globalization;

namespace Microsoft.AspNet.Localization.Internal
{
    public static class CultureUtilities
    {
        public static CultureInfo GetCultureFromName(string cultureName)
        {
            // Allow empty string values as they map to InvariantCulture, whereas null culture values will throw in
            // the CultureInfo ctor
            if (cultureName == null)
            {
                return null;
            }

            try
            {
                return new CultureInfo(cultureName);
            }
            catch (CultureNotFoundException)
            {
                return null;
            }
        }
    }
}
