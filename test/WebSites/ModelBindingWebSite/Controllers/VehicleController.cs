// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using Microsoft.AspNet.Mvc;
using ModelBindingWebSite.Services;
using ModelBindingWebSite.ViewModels;

namespace ModelBindingWebSite
{
    public class VehicleController : Controller
    {
        [HttpPut("/api/vehicles/{id}")]
        [Produces("application/json")]
        public object UpdateVehicleApi(
            [Range(1, 500)] int id,
            [FromBody] VehicleViewModel model,
            [FromServices] IVehicleService service,
            [FromHeader(Name = "X-TrackingId")] string trackingId)
        {
            if (!ModelState.IsValid)
            {
                return SerializeModelState();
            }

            service.Update(id, model, trackingId);

            return model;
        }

        [HttpPost("/dealers/{dealer.id:int}/update-vehicle")]
        public IActionResult UpdateDealerVehicle(VehicleWithDealerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("UpdateVehicle", model);
            }

            model.Update();
            return PartialView("UpdateSuccessful", model);
        }

        public IDictionary<string, IEnumerable<string>> SerializeModelState()
        {
            Response.StatusCode = (int)HttpStatusCode.BadRequest;

            return ModelState.Where(item => item.Value.Errors.Count > 0)
                             .ToDictionary(item => item.Key, item => item.Value.Errors.Select(e => e.ErrorMessage));
        }
    }
}