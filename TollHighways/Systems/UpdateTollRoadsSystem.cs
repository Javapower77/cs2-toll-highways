﻿using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Routes;
using Game.Serialization;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using Game.Vehicles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using TollHighways.Domain;
using TollHighways.Domain.Components;
using TollHighways.Jobs;
using TollHighways.Utilities;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using SubLane = Game.Net.SubLane;

namespace TollHighways.Systems
{
    public partial class UpdateTollRoadsSystem : GameSystemBase
    {
        private EntityQuery tollRoadsQuery;
        private NativeHashMap<Entity, Entity> lastVehiclesOnTollRoad; // TollRoadEntity -> VehicleEntity
        private PrefabSystem prefabSystem;
        private EntityQuery InsightQueryForTolls;

        protected override void OnCreate()
        {
            LogUtil.Info("TollHighways::UpdateTollRoadsSystem::OnCreate()");
            base.OnCreate();

            LogUtil.Info("TollHighways::UpdateTollRoadsSystem::OnCreate()::Call Function InitializeQueries()");
            InitializeQueries();

            LogUtil.Info("TollHighways::UpdateTollRoadsSystem::OnCreate()::Initializing lastVehiclesOnTollRoad");   
            lastVehiclesOnTollRoad = new NativeHashMap<Entity, Entity>(16, Allocator.Persistent);

            prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
        }

        // This method is to initialize the queries used in this system and cache them for later use.
        private void InitializeQueries()
        {
            // Query all toll roads that have the TollRoadPrefabData component.
            // This is used to identify the roads that are toll roads.
            tollRoadsQuery = SystemAPI.QueryBuilder()
                    .WithAll<Game.Prefabs.PrefabRef, TollRoadPrefabData>()
                    .WithNone<Game.Prefabs.RoadComposition>()
                    .Build();

            // Query all entities with TollInsights component
            InsightQueryForTolls = SystemAPI.QueryBuilder()
                .WithAll<TollInsights>()
                .Build();
        }

        protected override void OnUpdate()
        {
            //StartAsyncUpdate();           
            NativeList<(Entity tollRoad, Entity vehicle)> currentEntries = GetCurrentVehiclesOnTollRoad();

            foreach (var (tollRoad, vehicle) in currentEntries)
            {
                if (!lastVehiclesOnTollRoad.TryGetValue(tollRoad, out var lastVehicle) || lastVehicle != vehicle)
                {
                    // Vehicle just entered the toll road
                    TriggerTollAction(tollRoad, vehicle);
                    lastVehiclesOnTollRoad[tollRoad] = vehicle;
                }
            }
        }

        private void TriggerTollAction(Entity tollRoad, Entity vehicle)
        {
            TollHighways.Domain.Vehicle _vehicle = new();

            _vehicle.Type = GetVehicleType(vehicle);
            if (EntityManager.TryGetComponent(vehicle, out PrefabRef vehiclePrefab))
            {
                if (prefabSystem.TryGetPrefab<PrefabBase>(vehiclePrefab, out var prefab))
                {
                    _vehicle.Name = prefab.name;
                }
            }

            LogUtil.Info($"Vehicle {_vehicle.Name} of type {_vehicle.Type} with Index {vehicle.Index} had entered in the toll road Index {tollRoad.Index}");

            Entity insightEntity = Entity.Null;
            using (var insightEntities = InsightQueryForTolls.ToEntityArray(Allocator.Temp))
            {
                foreach (var entity in insightEntities)
                {
                    var insightData = EntityManager.GetComponentData<TollInsights>(entity);
                    // Check if the insight data matches the toll road and vehicle name
                    if (insightData.TollRoadPrefab.Equals(tollRoad) && insightData.VehicleName.Equals(_vehicle.Name) && insightData.VehicleType.Equals(_vehicle.Type))
                    {
                        insightEntity = entity;
                        break;
                    }
                }
            }

            if (insightEntity != Entity.Null)
            {
                // If the insight entity already exists, increment the pass-through count
                var insightData = EntityManager.GetComponentData<TollInsights>(insightEntity);
                insightData.PassThroughCount++;
                EntityManager.SetComponentData(insightEntity, insightData);
            }
            else
            {
                // If the insight entity does not exist, create a new one
                TollInsights newInsight = new()
                {
                    TollRoadPrefab = tollRoad,
                    VehicleType = _vehicle.Type,
                    VehicleName = _vehicle.Name,
                    PassThroughCount = 1
                };
                insightEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(insightEntity, newInsight);
            }

            List<TollInsights> insights = GetTollInsightsForPrefab(tollRoad);
        }

        public List<TollInsights> GetTollInsightsForPrefab(Entity tollRoadPrefabEntity)
        {
            var result = new List<TollInsights>();
            using (var entities = InsightQueryForTolls.ToEntityArray(Allocator.Temp))
            {
                foreach (var entity in entities)
                {
                    var insight = EntityManager.GetComponentData<TollInsights>(entity);
                    if (insight.TollRoadPrefab == tollRoadPrefabEntity)
                    {
                        result.Add(insight);
                    }
                }
            }
            return result;
        }

        /// <summary>
        ///  This function determines the type of vehicle based on the components present in the vehicle entity.
        /// </summary>
        /// <param name="vehicleEntity">
        /// The entity representing the vehicle.
        /// </param>
        /// <returns>
        /// Enumeration representing the type of vehicle defined in the TollHighways.Domain.Enums namespace.
        /// </returns>
        private Domain.Enums.VehicleType GetVehicleType(Entity vehicleEntity)
        {
            // Check if the vehicle has a trailer. Can be a car or a truck.
            if (SystemAPI.HasBuffer<Game.Vehicles.LayoutElement>(vehicleEntity))
            {
                // Get the vehicle layout elements to determine if it is a car or a truck
                if (EntityManager.TryGetBuffer<Game.Vehicles.LayoutElement>(vehicleEntity, true, out DynamicBuffer<LayoutElement> vehicleLayout))
                {
                    // The component LayoutElement is used to represent the vehicle layout,
                    // which can be used to determine if the trailer are attached to a car or a truck.
                    // Index position 1 can have a Car object or a DeliveryTruck object. This way we can determine
                    // if the vehicle is a car with a trailer or a truck with a trailer.
                    if (SystemAPI.HasComponent<Game.Vehicles.PersonalCar>(vehicleLayout[1].m_Vehicle))
                    {
                        return TollHighways.Domain.Enums.VehicleType.PersonalCarWithTrailer;
                    }
                    else
                    {
                        return TollHighways.Domain.Enums.VehicleType.TruckWithTrailer;
                    }
                }
            }
            // Check if the PublicTransport component is present, which indicates the vehicle type is a bus.
            else if (SystemAPI.HasComponent<Game.Vehicles.PublicTransport>(vehicleEntity))
            {
                return TollHighways.Domain.Enums.VehicleType.Bus;
            }
            // Check if DeliveryTruck component is present, which indicates the vehicle type is a truck.
            else if (SystemAPI.HasComponent<Game.Vehicles.DeliveryTruck>(vehicleEntity))
            {
                return TollHighways.Domain.Enums.VehicleType.Truck;
            }
            // Check if PersonalCar component is present, which indicates the vehicle type is a car.
            else if (SystemAPI.HasComponent<Game.Vehicles.PersonalCar>(vehicleEntity))
            {
                return TollHighways.Domain.Enums.VehicleType.PersonalCar;
            }
            // Check if PoliceCar component is present, which indicates the vehicle type is a police car.
            else if (SystemAPI.HasComponent<Game.Vehicles.PoliceCar>(vehicleEntity))
            {
                return TollHighways.Domain.Enums.VehicleType.PoliceCar;
            }
            // Check if GarbageTruck component is present, which indicates the vehicle type is a garbage truck.
            else if (SystemAPI.HasComponent<Game.Vehicles.GarbageTruck>(vehicleEntity))
            {
                return TollHighways.Domain.Enums.VehicleType.GarbageTruck;
            }
            // Check if Taxi component is present, which indicates the vehicle type is a taxi.
            else if (SystemAPI.HasComponent<Game.Vehicles.Taxi>(vehicleEntity))
            {
                return TollHighways.Domain.Enums.VehicleType.Taxi;
            }
            // To identify a motorcycle, we check if the vehicle has a Passenger component and does not have an Odometer component.
            // Having Odometer component means it is a taxi
            else if ((SystemAPI.HasBuffer<Game.Vehicles.Passenger>(vehicleEntity)) && (!SystemAPI.HasComponent<Game.Vehicles.Odometer>(vehicleEntity)))
            {
                return TollHighways.Domain.Enums.VehicleType.Motorcycle;
            }
            // Check if Ambulance component is present, which indicates the vehicle type is an ambulance.
            else if (SystemAPI.HasComponent<Game.Vehicles.Ambulance>(vehicleEntity))
            {
                return TollHighways.Domain.Enums.VehicleType.Ambulance;
            }
            // Check if FireEngine component is present, which indicates the vehicle type is a fire engine.
            else if (SystemAPI.HasComponent<Game.Vehicles.FireEngine>(vehicleEntity))
            {
                return TollHighways.Domain.Enums.VehicleType.FireEngine;
            }
            //
            else if (SystemAPI.HasComponent<Game.Vehicles.EvacuatingTransport>(vehicleEntity))
            {
                return TollHighways.Domain.Enums.VehicleType.EvacuatingTransport;
            }
            // Check if ParkMaintenanceVehicle component is present, which indicates the vehicle type is a park maintenance vehicle.
            else if (SystemAPI.HasComponent<Game.Vehicles.ParkMaintenanceVehicle>(vehicleEntity))
            {
                return TollHighways.Domain.Enums.VehicleType.ParkMaintenance;
            }
            // Check if RoadMaintenanceVehicle component is present, which indicates the vehicle type is a road maintenance vehicle.
            else if (SystemAPI.HasComponent<Game.Vehicles.RoadMaintenanceVehicle>(vehicleEntity))
            {
                return TollHighways.Domain.Enums.VehicleType.RoadMaintenance;
            }
            // Check if Hearse component is present, which indicates the vehicle type is a hearse.
            else if (SystemAPI.HasComponent<Game.Vehicles.Hearse>(vehicleEntity))
            {
                return TollHighways.Domain.Enums.VehicleType.Hearse;
            }
            // Check if PrisonerTransport component is present, which indicates the vehicle type is a prisoner transport.
            else if (SystemAPI.HasComponent<Game.Vehicles.PrisonerTransport>(vehicleEntity))
            {
                return TollHighways.Domain.Enums.VehicleType.PrisonerTransport;
            }
            // Last, check if PostVan component is present, which indicates the vehicle type is a post van.
            else if (SystemAPI.HasComponent<Game.Vehicles.PostVan>(vehicleEntity))
            {
                return TollHighways.Domain.Enums.VehicleType.PostVan;
            }
            
            // If no specific type is found, return None
            return TollHighways.Domain.Enums.VehicleType.None;
        }

        private NativeList<(Entity tollRoad, Entity vehicle)> GetCurrentVehiclesOnTollRoad()
        {
            NativeList<(Entity tollRoad, Entity vehicle)> currentEntries = new NativeList<(Entity, Entity)>(Allocator.TempJob);
            NativeArray<Entity> tollRoadEntities = tollRoadsQuery.ToEntityArray(Allocator.TempJob);

            // Create the job
            CalculateVehicleInTollRoads vehicleTollJob = new CalculateVehicleInTollRoads
            {
                tollRoadEntities = tollRoadEntities,
                EdgeObjectData = SystemAPI.GetComponentLookup<Game.Net.Edge>(true),
                LaneObjectData = SystemAPI.GetBufferLookup<Game.Net.LaneObject>(true),
                SubLaneObjectData = SystemAPI.GetBufferLookup<Game.Net.SubLane>(true),
                PrefabRefData = SystemAPI.GetComponentLookup<Game.Prefabs.PrefabRef>(true),
                VehicleTrailerData = SystemAPI.GetComponentLookup<Game.Vehicles.CarTrailerLane>(true),
                Results = currentEntries // Pass the currentEntries list to the job
            };

            // Schedule the job
            JobHandle vehicleTollJobHandle = vehicleTollJob.Schedule(tollRoadEntities.Length, 1);

            // Complete the job
            vehicleTollJobHandle.Complete();

            tollRoadEntities.Dispose(); // Dispose of the array after use

            return currentEntries;
        }
    }
}
