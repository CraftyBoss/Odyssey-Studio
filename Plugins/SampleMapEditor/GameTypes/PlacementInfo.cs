using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CafeLibrary;
using ByamlExt.Byaml;
using OpenTK;
using RedStarLibrary.MapData;

namespace RedStarLibrary.GameTypes
{
    public class PlacementInfo
    {

        // byaml dictionary data obtained from stage byml
        public Dictionary<string, dynamic> actorNode;
        // string used in SMO's object factory to create the LiveActor 
        public string ClassName
        {
            get { return actorNode["UnitConfig"]["ParameterConfigName"]; }
            set { actorNode["UnitConfig"]["ParameterConfigName"] = value; }
        }
        // name of actor's model
        public string ModelName
        {
            get { return actorNode["ModelName"]; }
            set { actorNode["ModelName"] = value; }
        }
        public string UnitConifgName
        {
            get { return actorNode["UnitConfigName"]; }
            set { actorNode["UnitConfigName"] = value; }
        }
        // object ID used to differentiate actors
        public string ObjID
        {
            get { return actorNode["Id"]; }
            set { actorNode["Id"] = value; }
        }
        // metadata leftover from official level editor used to handle cross-scenario objects
        public string LayerName
        {
            get { return actorNode["LayerConfigName"]; }
            set { actorNode["LayerConfigName"] = value; }
        }
        // List of all Linked objects used by actor, separated by list categories
        public Dictionary<string,dynamic> Links
        {
            get { return actorNode["Links"]; }
            set { actorNode["Links"] = value; }
        }
        // The Category that the actor's placement info is found in
        public string ActorCategory
        {
            get { return actorNode["UnitConfig"]["GenerateCategory"]; }
            set { actorNode["UnitConfig"]["GenerateCategory"] = value; }
        }
        // Bool used to describe whether or not placement info is the destination of a link
        public bool IsLinkDest
        {
            get { return actorNode["IsLinkDest"]; }
            set { actorNode["IsLinkDest"] = value; }
        }

        // object position
        public Vector3 translation;
        // object rotation
        public Vector3 rotation;
        // object scale
        public Vector3 scale;

        public bool isUseLinks = false;
        public PlacementInfo()
        {
            actorNode = new Dictionary<string, dynamic>()
            {
                {"Id", "" },
                {"IsLinkDest", false },
                {"LayerConfigName", "Common" },
                {"ModelName",""},
                {"PlacementFileName",""},
                {"UnitConfigName", "" },
                {"Links", new Dictionary<string,dynamic>() },
                {"Rotate", new Dictionary<string,dynamic>(){ { "X", 0.0f }, { "Y", 0.0f }, { "Z", 0.0f } } },
                {"Scale", new Dictionary<string,dynamic>(){ { "X", 1.0f }, { "Y", 1.0f }, { "Z", 1.0f } } },
                {"Translate", new Dictionary<string,dynamic>(){ { "X", 0.0f }, { "Y", 0.0f }, { "Z", 0.0f } } },
                {"UnitConfig", new Dictionary<string,dynamic>(){ 
                    { "DisplayName", "" },
                    { "GenerateCategory", "" },
                    { "ParameterConfigName", "" }, 
                    { "ParameterTargetFile", "" },
                    {"DisplayRotate", new Dictionary<string,dynamic>(){ { "X", 0.0f }, { "Y", 0.0f }, { "Z", 0.0f } } },
                    {"DisplayScale", new Dictionary<string,dynamic>(){ { "X", 1.0f }, { "Y", 1.0f }, { "Z", 1.0f } } },
                    {"DisplayTranslate", new Dictionary<string,dynamic>(){ { "X", 0.0f }, { "Y", 0.0f }, { "Z", 0.0f } } },
                } },
            };

            translation = LoadVector("Translate");
            rotation = LoadVector("Rotate");
            scale = LoadVector("Scale");

            isUseLinks = false;
        }

        public PlacementInfo(Dictionary<string, dynamic> rootActorNode)
        {
            actorNode = rootActorNode;

            translation = LoadVector("Translate");
            rotation = LoadVector("Rotate");
            scale = LoadVector("Scale");

            if (rootActorNode["Links"].Count > 0)
            {
                isUseLinks = true;
            }

        }
        public void SaveTransform()
        {
            SaveVector("Translate", translation);
            SaveVector("Rotate", rotation);
            SaveVector("Scale", scale);
        }
        private Vector3 LoadVector(string key)
        {
            return new Vector3(actorNode[key]["X"], actorNode[key]["Y"], actorNode[key]["Z"]);
        }
        private void SaveVector(string key, Vector3 vec)
        {
            actorNode[key]["X"] = vec.X;
            actorNode[key]["Y"] = vec.Y;
            actorNode[key]["Z"] = vec.Z;
        }

        public override bool Equals(object obj)
        {
            if(obj is PlacementInfo)
                return this.ObjID == ((PlacementInfo)obj).ObjID;
            return false;
        }

    }
}
