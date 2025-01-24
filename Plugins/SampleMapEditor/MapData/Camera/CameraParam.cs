using HakoniwaByml.Iter;
using HakoniwaByml.Writer;
using RedStarLibrary.Interfaces;
using RedStarLibrary.MapData.Camera.CameraParams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.MapData.Camera
{
    internal class CameraParam : IBymlSerializable
    {
        internal class CameraTicket
        {
            public string Class;
            public string Name;
            public ICameraParam Params;
        }

        public List<CameraTicket> DefaultTickets = new();
        public List<CameraTicket> StartTickets = new();
        public List<CameraTicket> MapTickets = new();

        public static readonly Dictionary<string, string> Translation = new Dictionary<string, string>()
        {
            { "制限付きフォロー", "CameraPoserFollowLimit"},
            { "制限付き平行", "CameraPoserFollowLimit"},
            { "2D平行", "CameraPoserFollowLimit"},
            { "固定", "al::CameraPoserFix"},
            { "完全固定", "al::CameraPoserFix"},
            { "出入口専用固定", "al::CameraPoserFix"},
            { "定点", "al::CameraPoserFixPoint"},
            { "その場定点", "al::CameraPoserFixPoint"},
            { "完全追従定点", "al::CameraPoserFixPoint"},
            { "レース", "al::CameraPoserRace"},
            { "レール移動", "al::CameraPoserRailMoveLookAt"},
            { "キノピオ探検隊", "al::CameraPoserKinopioBrigade"},
            { "会話用2点間", "al::CameraPoserTalk"},
            { "映像撮影レール", "al::CameraPoserRailMoveMovie"},
            { "ボス戦カメラ", "al::CameraPoserBossBattle"},
            { "スタート", "al::CameraPoserEntrance"},
            { "看板用2点間", "al::CameraPoserLookBoard"},
            { "見下ろし", "al::CameraPoserLookDown"},
            { "主観", "al::CameraPoserSubjective"},
            { "塔", "al::CameraPoserTower"},
            { "キー移動固定", "al::KeyMoveCameraFix"},
            { "キー移動レール移動", "al::KeyMoveCameraRailMove"},
            { "キー移動ズーム", "al::KeyMoveCameraZoom"},
            { "シナリオ紹介シンプルズームカメラ", "ScenarioStartCameraPoserSimpleZoom"},
            { "シナリオ紹介レール移動カメラ", "ScenarioStartCameraPoserRailMove"},
        };

        public void DeserializeByml(BymlIter rootNode)
        {
            foreach ((var ticketCategory, var ticketArr) in rootNode.As<BymlIter>())
            {
                if (!ticketArr.Iterable)
                    continue;

                List<CameraTicket> targetList;
                if (ticketCategory == "DefaultTickets")
                    targetList = DefaultTickets;
                else if (ticketCategory == "StartTickets")
                    targetList = StartTickets;
                else if (ticketCategory == "Tickets")
                    targetList = MapTickets;
                else
                    continue;

                foreach (var ticketIter in ticketArr.AsArray<BymlIter>())
                    targetList.Add(CreateTicket(ticketIter));
            }
        }

        public BymlContainer SerializeByml()
        {
            throw new NotImplementedException();
        }

        private CameraTicket CreateTicket(BymlIter ticketIter)
        {
            var ticket = new CameraTicket();

            ticketIter.TryGetValue("Class", out BymlIter classIter);
            if (classIter.TryGetValue("Name", out string name))
                ticket.Class = name;


            ticketIter.TryGetValue("Id", out BymlIter idIter);
            if (idIter.TryGetValue("Suffix", out string suffix))
                ticket.Name = name;
            else if (idIter.TryGetValue("ObjId", out string objId))
                ticket.Name = objId;

            ticketIter.TryGetValue("Param", out BymlIter paramIter);

            if(paramIter.Iterable)
                ticket.Params = CamParamFactory.TryGetCameraParam(name, paramIter);

            if (Translation.ContainsKey(name))
                name = Translation[name];

            CollectParams(name, ticket.Params);

            return ticket;
        }

        private void CollectParams(string className, ICameraParam paramData)
        {
            if(paramData is not UnknownParams paramDict) {
                return;
            }

            if(!EditorLoader.TempCameraParamCollection.TryGetValue(className, out Dictionary<string, string> paramEntries))
                EditorLoader.TempCameraParamCollection.Add(className, paramEntries = new Dictionary<string, string>());

            foreach (var paramEntry in paramDict.Params)
            {
                if (!paramEntries.ContainsKey(paramEntry.Key))
                    paramEntries.Add(paramEntry.Key, paramEntry.Value.GetType().Name);
            }
        }
    }
}