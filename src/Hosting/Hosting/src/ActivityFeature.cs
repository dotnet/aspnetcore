using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Default implementation for <see cref="IActivityFeature"/>.
    /// </summary>
    internal class ActivityFeature : IActivityFeature
    {
        private const int _maxActivityDepth = 5;
        private Activity? _cachedActivity = null;
        private bool _activityRetrieved;
        /// <inheritdoc />
        public Activity? Activity
        {
            get
            {
                if (!_activityRetrieved && _cachedActivity is null)
                {
                    _activityRetrieved = true;
                    var activity = Activity.Current;
                    if (activity is not null)
                    {
                        var depth = 0;
                        while (!Equals(activity.OperationName, HostingApplicationDiagnostics.ActivityName))
                        {
                            depth++;
                            if (activity.Parent is null || depth > _maxActivityDepth)
                            {
                                _cachedActivity = null;
                                return _cachedActivity;
                            }
                            activity = activity.Parent;
                        }
                        _cachedActivity = activity;
                    }
                    else
                    {
                        _cachedActivity = null;
                    }
                }
                return _cachedActivity;
            }
        }
    }
}
