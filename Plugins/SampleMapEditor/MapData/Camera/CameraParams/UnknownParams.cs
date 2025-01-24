using HakoniwaByml.Iter;
using HakoniwaByml.Writer;
using RedStarLibrary.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.MapData.Camera.CameraParams
{
    internal class UnknownParams : ICameraParam
    {
        public Dictionary<string, object?> Params { get; private set; }

        public void DeserializeByml(BymlIter rootNode) { Params = Placement.ConvertToDict(rootNode); }

        public BymlContainer SerializeByml() => Placement.ConvertToHash(Params);
    }
}
