// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.WebUtilities;
using ModelBindingWebSite.ViewModels;
using ModelBindingWebSite.Services;

namespace ModelBindingWebSite
{
    public class VehicleController : Controller
    {
        private static VehicleViewModel _vehicle = new VehicleViewModel
        {
            InspectedDates = new[]
            {
                // 01/04/2001 00:00:00 -08:00
                new DateTimeOffset(
                            year: 2001,
                            month: 4,
                            day: 1,
                            hour: 0,
                            minute: 0,
                            second: 0,
                            offset: TimeSpan.FromHours(-8)),
            },
            Make = "Fast Cars",
            Model = "the Fastener",
            Vin = "87654321",
            Year = 2013,
        };

        [HttpPut("/api/vehicles/{id}")]
        [Produces("application/json")]
        public object UpdateVehicleApi(
            int id,
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

        [HttpGet("/vehicles/{id:int}")]
        public IActionResult Details(int id)
        {
            if (id != 42)
            {
                return HttpNotFound();
            }

            return View(_vehicle);
        }

        [HttpGet("/vehicles/{id:int}/edit")]
        public IActionResult Edit(int id)
        {
            if (id != 42)
            {
                return HttpNotFound();
            }

            // Provide room for one additional inspection if not already full.
            var vehicle = _vehicle;
            var length = vehicle.InspectedDates.Length;
            if (length < 10)
            {
                var array = new DateTimeOffset[length + 1];
                vehicle.InspectedDates.CopyTo(array, 0);

                // Don't update the stored VehicleViewModel instance.
                vehicle = new VehicleViewModel
                {
                    InspectedDates = array,
                    LastUpdatedTrackingId = vehicle.LastUpdatedTrackingId,
                    Make = vehicle.Make,
                    Model = vehicle.Model,
                    Vin = vehicle.Vin,
                    Year = vehicle.Year,
                };
            }

            return View(vehicle);
        }

        [HttpPost("/vehicles/{id:int}/edit")]
        public IActionResult Edit(int id, VehicleViewModel vehicle)
        {
            if (id != 42)
            {
                return HttpNotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(vehicle);
            }

            if (vehicle.InspectedDates != null)
            {
                // Ignore empty inspection values.
                var nonEmptyDates = vehicle.InspectedDates.Where(date => date != default(DateTimeOffset)).ToArray();
                vehicle.InspectedDates = nonEmptyDates;
            }

            _vehicle = vehicle;

            return RedirectToAction(nameof(Details), new { id = id });
        }

        public IDictionary<string, IEnumerable<string>> SerializeModelState()
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;

            return ModelState.Where(item => item.Value.Errors.Count > 0)
                             .ToDictionary(item => item.Key, item => item.Value.Errors.Select(e => e.ErrorMessage));
        }
    }
}