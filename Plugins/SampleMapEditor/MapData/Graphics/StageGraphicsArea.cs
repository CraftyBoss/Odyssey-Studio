using HakoniwaByml.Iter;
using HakoniwaByml.Writer;
using OpenTK;
using RedStarLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RedStarLibrary.MapData.Graphics
{
    public class StageGraphicsArea : IBymlSerializable
    {
        public class AreaParam
        {
            public string AdditionalCubeMapArchiveName = "";
            public string AdditionalCubeMapUnitName = "";
            public string AreaName = "DefaultArea";
            public Vector3 BaseAngle = Vector3.Zero;
            public string CubeMapUnitName = "Default";
            public int LerpStep = 0;
            public int LerpStepOut = -1;
            public string PresetName = "";
            public string SuffixName = "";

            public bool IsValid => !string.IsNullOrEmpty(PresetName) && !string.IsNullOrEmpty(AreaName);

            public AreaParam() { }

        }

        public List<AreaParam> ParamArray = new List<AreaParam>();

        public StageGraphicsArea(BymlIter iter) { DeserializeByml(iter); }

        public void DeserializeByml(BymlIter gfxParam)
        {
            if (gfxParam.TryGetValue("GraphicsAreaParamArray", out BymlIter paramArray))
            {
                foreach (var graphicsIter in paramArray.AsArray<BymlIter>())
                {
                    AreaParam param = new AreaParam();

                    graphicsIter.TryGetValue("AdditionalCubeMapArchiveName", out param.AdditionalCubeMapArchiveName);
                    graphicsIter.TryGetValue("AdditionalCubeMapUnitName", out param.AdditionalCubeMapUnitName);
                    graphicsIter.TryGetValue("AreaName", out param.AreaName);

                    if(graphicsIter.TryGetValue("BaseAngle", out BymlIter baseAngleIter))
                        param.BaseAngle = Helpers.Placement.LoadVector(baseAngleIter);

                    graphicsIter.TryGetValue("CubeMapUnitName", out param.CubeMapUnitName);
                    graphicsIter.TryGetValue("LerpStep", out param.LerpStep);
                    graphicsIter.TryGetValue("LerpStepOut", out param.LerpStepOut);
                    graphicsIter.TryGetValue("PresetName", out param.PresetName);
                    graphicsIter.TryGetValue("SuffixName", out param.SuffixName);

                    // clean up unused area param entries
                    if(param.IsValid)
                        ParamArray.Add(param);
                }
            }
        }

        public BymlContainer SerializeByml()
        {
            BymlHash bymlHash = new BymlHash();

            BymlArray paramArray = new BymlArray();
            bymlHash.Add("GraphicsAreaParamArray", paramArray);

            foreach (var param in ParamArray)
            {
                BymlHash paramIter = new BymlHash();

                paramIter.Add("AdditionalCubeMapArchiveName", param.AdditionalCubeMapArchiveName);
                paramIter.Add("AdditionalCubeMapUnitName", param.AdditionalCubeMapUnitName);
                paramIter.Add("AreaName", param.AreaName);
                paramIter.Add("BaseAngle", Helpers.Placement.SaveVector(param.BaseAngle));
                paramIter.Add("CubeMapUnitName", param.CubeMapUnitName);
                paramIter.Add("LerpStep", param.LerpStep);
                paramIter.Add("LerpStepOut", param.LerpStepOut);
                paramIter.Add("PresetName", param.PresetName);
                paramIter.Add("SuffixName", param.SuffixName);

                paramArray.Add(paramIter);
            }

            return bymlHash;
        }

        public AreaParam TryGetDefaultAreaParam() => ParamArray.FirstOrDefault(e => e.AreaName == "DefaultArea");
        public AreaParam TryGetScenarioParam(int scenIdx) => ParamArray.FirstOrDefault(e => e.AreaName == $"Scenario{scenIdx+1}");

        public AreaParam AddNewParam()
        {
            var newParam = new AreaParam();
            ParamArray.Add(newParam);
            return newParam;
        }
    }
}
