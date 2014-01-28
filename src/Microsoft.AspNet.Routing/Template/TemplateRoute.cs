using System;
using System.Collections.Generic;
using Microsoft.AspNet.Routing.Legacy;

namespace Microsoft.AspNet.Routing.Template
{
    public class TemplateRoute : IRoute
    {
        public TemplateRoute(IRouteEndpoint destination, string template)
            : this(destination, template, null, null, null)
        {
        }

        public TemplateRoute(IRouteEndpoint destination, string template, IDictionary<string, object> defaults, IDictionary<string, object> constraints, IDictionary<string, object> data)
        {
            this.Destination = destination;
            this.Template = template;
            this.Defaults = defaults;
            this.Constraints = constraints;
            this.Data = data;

            this.ParsedRoute = RouteParser.Parse(template);

            this.Initialize();
        }

        private IDictionary<string, object> Constraints
        {
            get;
            set;
        }

        private IDictionary<string, object> Data
        {
            get;
            set;
        }

        private IRouteEndpoint Destination
        {
            get;
            set;
        }

        public IDictionary<string, object> FilterValues
        {
            get
            {
                return this.Defaults;
            }
        }

        private IDictionary<string, object> Defaults
        {
            get;
            set;
        }

        private List<KeyValuePair<string, IConstraint>> ConstraintsInternal
        {
            get;
            set;
        }

        private HttpParsedRoute ParsedRoute
        {
            get;
            set;
        }

        private string Template
        {
            get;
            set;
        }

        public RouteMatch GetMatch(RoutingContext context)
        {
            var match = this.ParsedRoute.Match(context, this.Defaults);
            if (match == null)
            {
                return null;
            }

            for (int i = 0; i < this.ConstraintsInternal.Count; i++)
            {
                var kvp = this.ConstraintsInternal[i];

                object value = null;
                if (!String.IsNullOrEmpty(kvp.Key))
                {
                    match.TryGetValue(kvp.Key, out value);
                }

                if (!kvp.Value.MatchInbound(context, match, value))
                {
                    return null;
                }
            }

            return new RouteMatch(this.Destination.AppFunc, match);
        }

        public bool OnSelected(IDictionary<string, object> context, RouteMatch match)
        {
            if (this.Data != null)
            {
                foreach (var kvp in this.Data)
                {
                    context[kvp.Key] = kvp.Value;
                }
            }

            return true;
        }

        public BoundRoute Bind(RouteBindingContext context)
        {
            var template = this.ParsedRoute.Bind(context, this.Defaults);
            if (template == null)
            {
                return null;
            }

            for (int i = 0; i < this.ConstraintsInternal.Count; i++)
            {
                var kvp = this.ConstraintsInternal[i];

                object value = null;
                if (!String.IsNullOrEmpty(kvp.Key))
                {
                    template.Values.TryGetValue(kvp.Key, out value);
                }

                if (!kvp.Value.MatchOutbound(context, template.Values, value))
                {
                    return null;
                }
            }

            return new BoundRoute(template.BoundTemplate, template.Values);
        }

        private void Initialize()
        {
            this.ConstraintsInternal = new List<KeyValuePair<string, IConstraint>>();

            if (this.Constraints == null)
            {
                return;
            }

            foreach (var kvp in this.Constraints)
            {
                string constraintString;
                IConstraint constraint;

                if ((constraintString = kvp.Value as string) != null)
                {
                    // TODO regex constraints
                }
                else if ((constraint = kvp.Value as IConstraint) != null)
                {
                    this.ConstraintsInternal.Add(new KeyValuePair<string, IConstraint>(kvp.Key, constraint));
                }
                else
                {
                    throw new ArgumentException();
                }
            }
        }
    }
}
