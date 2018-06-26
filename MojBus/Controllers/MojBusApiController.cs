﻿using Microsoft.AspNetCore.Mvc;
using MojBus.Data;
using MojBus.Helpers;
using System;

namespace MojBus.Controllers
{
    [Produces("application/json")]
    [Route("api/[action]")]
    public class MojBusApiController : Controller
    {
        MojBusContext _context;

        public MojBusApiController(MojBusContext context)
        {
            _context = context;
        }

        public IActionResult Stops()
        {
            return Json(MojBusHelper.GetStops(_context));
        }

        public IActionResult Routes()
        {
            return Json(MojBusHelper.GetRoutes(_context));
        }

        [HttpGet]
        public IActionResult StopData(int stopId, DateTime date)
        {
            return Json(MojBusHelper.StopTimesForStop(_context, stopId, date));
        }

        [HttpGet]
        public IActionResult StopDataForRoute(int stopId, int routeId, DateTime date)
        {
            return Json(MojBusHelper.StopTimesForStop(_context, stopId, routeId, date));
        }
    }
}