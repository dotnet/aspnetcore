// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Xml;

namespace FiltersWebSite
{
    public class ModifyResultsFilterAttribute : ResultFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            var objResult = context.Result as ObjectResult;
            var dummyClass = objResult.Value as DummyClass;
            dummyClass.SampleInt = 120;

            objResult.Formatters.Add(new XmlSerializerOutputFormatter());

            base.OnResultExecuting(context);
        }
    }
}