using HakoniwaByml.Iter;
using HakoniwaByml.Writer;
using RedStarLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;

namespace RedStarLibrary.MapData
{
    public class WorldList : IBymlSerializable
    {
        public class Entry
        {
            public record struct StageEntry(string category, string name);

            public int AfterEndingScenario = 0;
            public int ClearMainScenario = 0;
            public List<int> MainQuestInfo = new();
            public int MoonRockScenario = 0;
            public string Name = "";
            public int ScenarioNum = 0;
            public List<StageEntry> StageList = new();
            public string WorldName = "";

            public Entry() { }
            public Entry(BymlIter iter)
            {
                iter.TryGetValue("AfterEndingScenario", out AfterEndingScenario);
                iter.TryGetValue("ClearMainScenario", out ClearMainScenario);
                iter.TryGetValue("MoonRockScenario", out MoonRockScenario);
                iter.TryGetValue("Name", out Name);
                iter.TryGetValue("ScenarioNum", out ScenarioNum);
                iter.TryGetValue("WorldName", out WorldName);

                if(iter.TryGetValue("MainQuestInfo", out BymlIter infosIter))
                {
                    if (infosIter.GetSize() != ScenarioNum)
                        StudioLogger.WriteWarning("MainQuestInfo iter size does not match ScenarioNum!");

                    foreach (var scenarioIdx in infosIter.AsArray<int>())
                        MainQuestInfo.Add(scenarioIdx);
                }

                if (iter.TryGetValue("StageList", out BymlIter stageListIter))
                {
                    foreach (var stageEntryIter in stageListIter.AsArray<BymlIter>())
                    {
                        stageEntryIter.TryGetValue("category", out string category);
                        stageEntryIter.TryGetValue("name", out string name);

                        StageList.Add(new StageEntry(category, name));
                    }
                }
            }
        }

        public List<Entry> WorldEntries = new();

        public void DeserializeByml(BymlIter rootNode)
        {
            foreach (var worldEntryIter in rootNode.AsArray<BymlIter>())
                WorldEntries.Add(new Entry(worldEntryIter));
        }

        public BymlContainer SerializeByml()
        {
            throw new NotImplementedException();
        }
    }
}
