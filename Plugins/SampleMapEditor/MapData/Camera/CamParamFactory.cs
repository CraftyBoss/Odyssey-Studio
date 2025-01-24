using HakoniwaByml.Iter;
using RedStarLibrary.MapData.Camera.CameraParams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.MapData.Camera
{
    internal class CamParamFactory
    {
        public delegate ICameraParam ParamCreator(BymlIter iter);

        private static Dictionary<string, ParamCreator> factoryEntries = new Dictionary<string, ParamCreator>()
        {

        };

        public static ICameraParam TryGetCameraParam(string className, BymlIter paramIter)
        {
            if (factoryEntries.TryGetValue(className, out ParamCreator creator))
                return creator(paramIter);
            var unkParam = new UnknownParams();
            unkParam.DeserializeByml(paramIter);
            return unkParam;
        }
    }
}
