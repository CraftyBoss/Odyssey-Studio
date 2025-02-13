using ImGuiNET;
using RedStarLibrary.GameTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.MapData
{
    public class LayerConfig
    {
        /// <summary>
        /// Name of the config.
        /// </summary>
        public string LayerName { get;}
        /// <summary>
        /// List of every PlacementInfo found within a certain layer.
        /// </summary>
        public List<PlacementInfo> LayerObjects { get; }
        /// <summary>
        /// Specifies whether or not Layer is currently active in the scene.
        /// </summary>
        public bool IsEnabled;

        /// <summary>
        /// List of all scenarios this layer is enabled/disabled in.
        /// </summary>
        private bool[] activeScenarios = new bool[StageScene.SCENARIO_COUNT];

        public LayerConfig(PlacementInfo actorPlacement)
        {
            LayerName = actorPlacement.LayerConfigName;
            LayerObjects = new List<PlacementInfo>() { actorPlacement };
            IsEnabled = true;
        }

        public LayerConfig(string layerName)
        {
            LayerName = layerName;
            LayerObjects = new List<PlacementInfo>();
            IsEnabled = true;
        }

        public bool IsInfoInLayer(PlacementInfo info) => LayerObjects.Any(e => e == info);
        public void SetScenarioActive(int idx, bool active) => activeScenarios[idx] = active;
        public bool IsScenarioActive(int idx) => activeScenarios[idx];
        public IEnumerable<PlacementInfo> GetPlacementsInScenario(int idx) => LayerObjects.Where(e=> e.IsScenarioActive(idx));

        internal void DrawScenarioTable()
        {
            if (ImGui.BeginTable("LayerScenarioTable", StageScene.SCENARIO_COUNT, ImGuiTableFlags.RowBg))
            {
                ImGui.TableNextRow();

                for (int i = 0; i < activeScenarios.Length; i++)
                {
                    ImGui.TableSetColumnIndex(i);
                    ImGui.Checkbox(i.ToString(), ref activeScenarios[i]);
                }

                ImGui.EndTable();
            }
        }
    }
}
