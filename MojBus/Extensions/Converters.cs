﻿using MojBus.Data.Entities;
using MojBus.Models;
using System.Collections.Generic;
using System.Linq;

namespace MojBus.Extensions
{
    public static class Converters
    {
        /// <summary>
        /// Maps table like list StopDataEntity into object like representation StopDataModel.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="stopName"></param>
        /// <returns></returns>
        public static List<StopDataModel> StopDataEntityToModel(this List<StopDataEntity> data, string stopName)
        {
            StopDataModel previous = null;
            List<StopDataModel> mappedData = new List<StopDataModel>();
            foreach (var item in data)
            {
                if (previous == null)
                {
                    previous = new StopDataModel()
                    {
                        StopLat = item.StopLat,
                        StopLon = item.StopLon,
                        HTMLColor = item.HTMLColor,
                        StopName = stopName,
                        TripShortName = item.TripShortName,
                        TripHeadsign = item.TripHeadsign,
                        DirectionID = item.DirectionID,
                        RouteId = item.RouteId,
                        RouteShortName = item.RouteShortName,
                        DepartureTimes = new List<string>() { item.DepartureTime.Trim() }
                    };
                }
                else if (item.TripHeadsign != previous.TripHeadsign ||
                    item.TripShortName != previous.TripShortName ||
                    item.DirectionID != previous.DirectionID)
                {
                    mappedData.Add(previous);
                    previous = new StopDataModel()
                    {
                        StopLat = item.StopLat,
                        StopLon = item.StopLon,
                        HTMLColor = item.HTMLColor,
                        StopName = stopName,
                        TripShortName = item.TripShortName,
                        TripHeadsign = item.TripHeadsign,
                        DirectionID = item.DirectionID,
                        RouteId = item.RouteId,
                        RouteShortName = item.RouteShortName,
                        DepartureTimes = new List<string>() { item.DepartureTime.Trim() }
                    };
                }
                else
                    previous.DepartureTimes.Add(item.DepartureTime.Trim());
            }
            mappedData.Add(previous);

            foreach (var item in mappedData)
                if (item != null)
                    item.DepartureTimes = item.DepartureTimes.OrderDepartureTimeAscending();

            return mappedData;
        }

        /// <summary>
        /// Groups list of stops by their trip short name, merges and sorts their stop times.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static List<StopDataModel> GroupByTripShortName(this List<StopDataModel> data)
        {
            if (data.First() == null)
                return new List<StopDataModel>();

            List<StopDataModel> temp = new List<StopDataModel>();
            StopDataModel previous = null;

            foreach (string distinctedTripShortName in data.Select(x => x.TripShortName).Distinct())
            {
                foreach (StopDataModel item in data.Where(x => x.TripShortName == distinctedTripShortName))
                {
                    if (previous == null)
                    {
                        previous = item;
                        previous.TripHeadsign = "";
                        continue;
                    }
                    previous.DepartureTimes.AddRange(item.DepartureTimes);
                }
                previous.DepartureTimes = previous.DepartureTimes.OrderBy(x => x).ToList();
                temp.Add(previous);
                previous = null;
            }

            return temp;
        }

        public static List<StopDataModel> AddFavouritesToStops(this List<StopDataModel> data, List<FavouriteStopRoutes> favourites)
        {
            foreach (StopDataModel stopData in data)
            {
                foreach (FavouriteStopRoutes route in favourites)
                {
                    if (stopData.TripShortName == route.RouteShortName)
                    {
                        stopData.isFavourite = true;
                    }
                }
            }

            return data;
        }

        public static List<StopTimes> RouteStopsEntityToStopTimesModel(this List<RouteStopsEntity> data)
        {
            List<StopTimes> routeStops = new List<StopTimes>();

            if (data.First() == null)
                return routeStops;

            int counter = 1;
            var groupedById = data.GroupBy(x => x.TripID).ToList();
            var distinctedStops = data.Select(x => x.StopName).Distinct().ToList();

            groupedById.ForEach(x => x.GroupBy(y => y.StopSequence));

            //TODO: FIND CORRECT ORDER FOR STOPS???
            foreach (var key in groupedById.ToList())
            {
                for (int i = 0; i < key.Count(); i++)
                {
                    string tempStopName = key.ElementAt(i).StopName;
                    if (distinctedStops[i] == tempStopName)
                        continue;
                    distinctedStops.Insert(i, tempStopName);
                    distinctedStops.Remove(tempStopName);
                }
            }

            foreach (string stopName in distinctedStops)
            {
                routeStops.Add(new StopTimes()
                {
                    StopId = data.First(x => x.StopName == stopName).Id,
                    StopName = stopName,
                    DepartureTimes = data
                    .Where(x => x.StopName == stopName)
                    .Select(x => x.DepartureTime.Trim())
                    .OrderBy(x => x)
                    .ToList()
                });
                counter++;
            }

            return routeStops;
        }

        public static List<RouteStopsModel> RouteStopsEntityToModel(this List<RouteStopsEntity> data)
        {
            List<RouteStopsModel> routeStops = new List<RouteStopsModel>();

            if (data.First() == null)
                return routeStops;

            foreach (int tripId in data.Select(x => x.TripID).Distinct())
            {
                IEnumerable<RouteStopsEntity> routeStopsFiltered = data.Where(x => x.TripID == tripId);
                List<StopTimeData> stopTimes = new List<StopTimeData>();

                foreach (RouteStopsEntity routeStop in routeStopsFiltered)
                {
                    stopTimes.Add(new StopTimeData()
                    {
                        DepartureTime = routeStop.DepartureTime.Trim(),
                        StopName = routeStop.StopName,
                        StopSequence = routeStop.StopSequence
                    });
                }

                routeStops.Add(new RouteStopsModel()
                {
                    TripID = tripId,
                    DirectionID = routeStopsFiltered.First().DirectionID,
                    Stops = stopTimes.OrderBy(x => x.StopSequence).ToList()
                });
            }

            return routeStops
                .OrderBy(x => x.Stops.First().DepartureTime)
                .ToList();
        }

        public static List<StopLocationModel> ConvertToRouteStopsDistinctedByStops(this List<RouteStopsEntity> data)
        {
            List<StopLocationModel> StopLocationsForRoute = new List<StopLocationModel>();

            foreach (var item in data.Select(x => new { x.StopLat, x.StopLon }).Distinct())
            {
                StopLocationsForRoute.Add(new StopLocationModel() { StopLat = item.StopLat, StopLon = item.StopLon });
            }

            return StopLocationsForRoute;
        }

        public static TripPlannerModel ConvertToTripPlannerModel(this List<TripPlannerEntity> data)
        {
            TripPlannerModel TripPlannerData = new TripPlannerModel();

            if (data.Count == 0 || data.First() == null)
                return TripPlannerData;

            TripPlannerEntity temp = data.First();
            TripPlannerData.StartStop = new StopModel()
            {
                StopDirection = temp.StartStopDirection,
                StopId = temp.StartStopId,
                StopLat = temp.StartStopLat,
                StopLon = temp.StartStopLon,
                StopName = temp.StartStopName.Trim()
            };
            TripPlannerData.EndStop = new StopModel()
            {
                StopDirection = temp.EndStopDirection,
                StopId = temp.EndStopId,
                StopLat = temp.EndStopLat,
                StopLon = temp.EndStopLon,
                StopName = temp.EndStopName.Trim()
            };

            List<Line> lines = new List<Line>();
            List<Trip> trips;
            List<TransitTime> transitTimes;

            foreach (var groupedByTripShortName in data.GroupBy(x => x.TripShortName))
            {
                trips = new List<Trip>();
                foreach (var groupedByTripHeadsign in groupedByTripShortName.GroupBy(x => x.TripHeadsign))
                {
                    transitTimes = new List<TransitTime>();
                    foreach (var item in groupedByTripHeadsign.ToList())
                    {
                        transitTimes.Add(new TransitTime()
                        {
                            ArrivalTime = item.EndStopArrivalTime.Trim(),
                            DepartureTime = item.StartStopDepartureTime.Trim(),
                            TravelTimeMinutes = item.TravelTimeMinutes
                        });
                    }
                    trips.Add(new Trip()
                    {
                        TripDirectionID = groupedByTripHeadsign.First().TripDirectionID,
                        TripHeadsign = groupedByTripHeadsign.First().TripHeadsign,
                        Times = transitTimes
                    });
                }
                lines.Add(new Line()
                {
                    HTMLColor = groupedByTripShortName.First().HTMLColor,
                    RouteShortName = groupedByTripShortName.First().RouteShortName,
                    TripShortName = groupedByTripShortName.First().TripShortName,
                    Trips = trips
                });
            }
            TripPlannerData.Lines = lines;

            return TripPlannerData;
        }

        public static List<string> OrderDepartureTimeAscending(this List<string> data)
        {
            List<string> orderedTimes = data.OrderBy(x => x).ToList();
            List<string> temp = data.TakeWhile(x => x.Contains("+")).ToList();
            foreach (string time in temp)
            {
                orderedTimes.Remove(time);
            }
            orderedTimes.AddRange(temp);

            return orderedTimes;
        }
    }
}
