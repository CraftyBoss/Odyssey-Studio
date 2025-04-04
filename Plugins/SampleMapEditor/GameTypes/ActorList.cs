﻿using IONET.Collada.Core.Scene;
using IONET.Collada.Kinematics.Kinematics_Models;
using RedStarLibrary.MapData;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RedStarLibrary.GameTypes
{
    public class ActorList : IEnumerable<LiveActor>
    {

        public string ActorListName;
        IList<LiveActor> _actorList;

        public PlacementList ActorPlacements { get; set; } 

        public ActorList(string listName)
        {
            ActorListName = listName;
            _actorList = new List<LiveActor>();
        }

        public ActorList(PlacementList list) : this(list.Name)
        {
            ActorPlacements = list;
        }

        public ActorList(PlacementList list, StageScene scene) : this(list.Name)
        {
            foreach (var placement in list.Placements)
                Add(scene.GetOrCreateLinkActor(placement));

            ActorPlacements = list;
        }

        public LiveActor this[int index]
        {
            get => _actorList[index];
            set => _actorList[index] = value;
        }
        public IEnumerator<LiveActor> GetEnumerator()
        {
            foreach (LiveActor actor in _actorList)
            {
                yield return actor;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(LiveActor actor)
        {
            if(actor != null)
            {
                _actorList.Add(actor);

                if (ActorPlacements != null)
                    ActorPlacements.Add(actor.Placement);
            }
        }

        public void AddRange(IEnumerable<LiveActor> actors)
        {
            if(actors != null)
                foreach (var actor in actors)
                    _actorList.Add(actor);
        }

        public void Clear() => _actorList.Clear();

        public bool IsContainActor(PlacementId objID) => _actorList.Any(e => e.Placement == objID);

        public bool IsContainActor(PlacementInfo objID) => _actorList.Any(e => e.Placement == objID);

        public LiveActor GetActorByPlacement(PlacementInfo info)
        {
            return _actorList.FirstOrDefault(e => e.Placement == info);
        }

        public void UpdateAllActorPlacement()
        {
            foreach (var actor in _actorList)
            {
                var placementInfo = actor.Placement;

                placementInfo.Translate = actor.Transform.Position;
                placementInfo.Rotate = actor.Transform.RotationEulerDegrees;
                placementInfo.Scale = actor.Transform.Scale;

            }
        }

    }
}
