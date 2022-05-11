using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.GameTypes
{
    public class ActorList : IEnumerable<LiveActor>
    {

        public string ActorListName;
        IList<LiveActor> _actorList;

        public ActorList(string listName)
        {
            ActorListName = listName;
            _actorList = new List<LiveActor>();
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
            }
        }

        public bool isContainActor(string objID)
        {
            return _actorList.Any(e => e.placement.ObjID == objID);
        }

        public void UpdateAllActorPlacement()
        {
            foreach (var actor in _actorList)
            {
                var placementInfo = actor.placement;

                placementInfo.translation = actor.Transform.Position;
                placementInfo.rotation = actor.Transform.RotationEulerDegrees;
                placementInfo.scale = actor.Transform.Scale;

                placementInfo.SaveTransform();
            }
        }

    }
}
