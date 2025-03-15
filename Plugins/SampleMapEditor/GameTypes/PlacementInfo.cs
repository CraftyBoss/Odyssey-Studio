using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using RedStarLibrary.MapData;
using Toolbox.Core;
using HakoniwaByml.Iter;
using HakoniwaByml.Writer;

namespace RedStarLibrary.GameTypes
{
    public class PlacementInfo : IEquatable<PlacementInfo>, IEquatable<PlacementId>, Interfaces.IBymlSerializable
    {
        public class PlacementUnitConfig
        {
            // (next 4 fields) Data used for display models an actor uses
            // (if an actor uses a display actor instead of directly being a model, most of the time these are used for transform info)
            [BindGUI("Display Name", Category = "Unit Config")]
            public string DisplayName { get; set; }
            [BindGUI("Display Rotate", Category = "Unit Config")]
            public Vector3 DisplayRotate { get; set; } = Vector3.Zero;
            [BindGUI("Display Scale", Category = "Unit Config")]
            public Vector3 DisplayScale { get; set; } = Vector3.One;
            /// <summary>
            /// Offset from the root actor's Translate
            /// </summary>
            [BindGUI("Display Offset", Category = "Unit Config")]
            public Vector3 DisplayTranslate { get; set; } = Vector3.Zero;
            /// <summary>
            /// placement category this info is found in.
            /// </summary>
            [BindGUI("Placement Category", Category = "Unit Config")]
            public string GenerateCategory { get; set; } = "";
            /// <summary>
            /// string used for constructing LiveActor using the game's ProjectActorFactory
            /// </summary>
            [BindGUI("Class Name", Category = "Unit Config")]
            public string ParameterConfigName { get; set; } = "";
            /// <summary>
            /// suffix of stage file (used for saving placement info to the correct Map/Design/Sound file possibly?)
            /// </summary>
            public string PlacementTargetFile { get; set; } = "";
        }

        // byaml dictionary data obtained from stage byml
        // public Dictionary<string, dynamic> actorNode;

        /// <summary>
        /// object ID used to differentiate actors
        /// </summary>
        [BindGUI("Object ID", Category = "Placement Info")]
        public string Id { get; set; } = "";
        /// <summary>
        /// Bool used to describe whether or not placement info is the destination of a link
        /// </summary>
        [BindGUI("Is Link Destination", Category = "Placement Info")]
        public bool IsLinkDest { get; set; } = false;
        /// <summary>
        /// metadata leftover from official level editor used to handle cross-scenario objects
        /// </summary>
        [BindGUI("Object Layer", Category = "Placement Info")]
        public string LayerConfigName { get; set; } = "";
        /// <summary>
        /// List of all Linked objects used by actor, separated by list categories
        /// </summary>
        public Dictionary<string, List<PlacementInfo>> Links { get; set; } = new();
        /// <summary>
        /// name of actor's model
        /// </summary>
        [BindGUI("Object Model", Category = "Placement Info")]
        public string ModelName { get; set; } = "";
        /// <summary>
        /// name of stage this placement info is found in
        /// </summary>
        [BindGUI("Placement Stage", Category = "Placement Info")]
        public string PlacementFileName { get; set; } = "";
        /// <summary>
        /// object rotation
        /// </summary>
        public Vector3 Rotate { get; set; } = Vector3.Zero;
        /// <summary>
        /// object scale
        /// </summary>
        public Vector3 Scale { get; set; } = Vector3.One;
        /// <summary>
        /// object position
        /// </summary>
        public Vector3 Translate { get; set; } = Vector3.Zero;
        /// <summary>
        /// general data used for additonal info relating to the actor/placement info.
        /// </summary>
        [BindGUI("Unit Config", Category = "Unit Config")]
        public PlacementUnitConfig UnitConfig { get; set; }
        /// <summary>
        /// name used for the LiveActor's constructor.
        /// </summary>
        [BindGUI("Actor Name", Category = "Placement Info")]
        public string UnitConfigName { get; set; }

        /// <summary>
        /// Optional string used for commenting about this placement info (god why did it have to be lowercase)
        /// </summary>
        [BindGUI("Comment", Category = "Placement Info")]
        public string Comment { get; set; }

        // helper properties (these are essentially the games al::tryGetX(value, al::PlacementInfo))
        public string ClassName { get { return UnitConfig.ParameterConfigName; } set { UnitConfig.ParameterConfigName = value; } }
        public string DisplayName { get { return UnitConfig.DisplayName; } set { UnitConfig.DisplayName = value; } }
        public string ObjectName { get { return UnitConfigName; } set { UnitConfigName = value; } }
        public string PlacementTargetFile { get { return UnitConfig.PlacementTargetFile; } set { UnitConfig.PlacementTargetFile = value; } }
        public Vector3 DisplayOffset { get { return UnitConfig.DisplayTranslate; } set { UnitConfig.DisplayTranslate = value; } }
        public Vector3 DisplayRotate { get { return UnitConfig.DisplayRotate; } set { UnitConfig.DisplayRotate = value; } }
        public Vector3 DisplayScale { get { return UnitConfig.DisplayScale; } set { UnitConfig.DisplayScale = value; } }

        public Dictionary<string, dynamic> ActorParams;

        public bool isActorLoaded = false;

        public bool isUseLinks = false;

        public bool isSyncInfoToLayer = true; // if disabled, placement info will only save itself in the scenario currently loaded

        private bool[] activeLayers = new bool[StageScene.SCENARIO_COUNT];

        private List<string> loadedParams;
        public PlacementInfo()
        {
            UnitConfig = new PlacementUnitConfig();

            loadedParams = new List<string>();

            ActorParams = new Dictionary<string, dynamic>();

            isUseLinks = false;
        }

        public PlacementInfo(BymlIter actorIter)
        {
            UnitConfig = new PlacementUnitConfig();

            loadedParams = new List<string>();

            DeserializeByml(actorIter);
        }

        public PlacementInfo(ObjectDatabaseEntry objEntry, string assetName)
        {
            UnitConfig = new PlacementUnitConfig();
            ActorParams = new Dictionary<string, dynamic>();
            isUseLinks = false;

            // load default placement info data
            ClassName = objEntry.ClassName;
            ObjectName = assetName;
            UnitConfig.PlacementTargetFile = objEntry.PlacementCategory;
            UnitConfig.GenerateCategory = objEntry.ActorCategory + "List";
            ModelName = assetName;

            // load required params 
            foreach (var param in objEntry.ActorParams)
            {
                var entry = param.Value;
                if (entry.Required)
                    ActorParams.Add(param.Key, entry.FoundValues.FirstOrDefault());
            }
        }

        public PlacementInfo(PlacementInfo other)
        {
            Id = other.Id;
            IsLinkDest = other.IsLinkDest;
            LayerConfigName = other.LayerConfigName;
            Links = Helpers.Placement.CopyNode(other.Links);
            ModelName = other.ModelName;
            PlacementFileName = other.PlacementFileName;
            Rotate = other.Rotate;
            Scale = other.Scale;
            Translate = other.Translate;

            UnitConfig = new PlacementUnitConfig();
            UnitConfig.DisplayName = other.UnitConfig.DisplayName;
            UnitConfig.DisplayRotate = other.UnitConfig.DisplayRotate;
            UnitConfig.DisplayScale = other.UnitConfig.DisplayScale;
            UnitConfig.DisplayTranslate = other.UnitConfig.DisplayTranslate;
            UnitConfig.GenerateCategory = other.UnitConfig.GenerateCategory;
            UnitConfig.ParameterConfigName = other.UnitConfig.ParameterConfigName;
            UnitConfig.PlacementTargetFile = other.UnitConfig.PlacementTargetFile;

            UnitConfigName = other.UnitConfigName;
            Comment = other.Comment;
            ActorParams = Helpers.Placement.CopyNode(other.ActorParams);
            isActorLoaded = other.isActorLoaded;
            isUseLinks = other.isUseLinks;
            isSyncInfoToLayer = other.isSyncInfoToLayer;
            Array.Copy(other.activeLayers, activeLayers, activeLayers.Length);

            loadedParams = Helpers.Placement.CopyNode(other.loadedParams);
        }

        public void SetScenarioActive(int idx, bool active) => activeLayers[idx] = active;
        public bool IsScenarioActive(int idx) => activeLayers[idx];
        public void SetActiveScenarios(LayerConfig config)
        {
            for (int i = 0; i < activeLayers.Length; i++)
                activeLayers[i] = config.IsScenarioActive(i);
        }
        public bool IsScenariosMatch(bool[] activeList)
        {
            if(activeList.Length != activeLayers.Length) return false;

            bool isMatch = true;

            for (int i = 0; i < activeLayers.Length; i++)
            {
                if(activeLayers[i] != activeList[i])
                {
                    isMatch = false; 
                    break;
                }
            }

            return isMatch;
        }

        private Dictionary<string, dynamic> GetActorParams(BymlIter iter)
        {
            Dictionary<string, dynamic> dict = new Dictionary<string, dynamic>();

            foreach ((string key, object? value) in iter)
            {
                if (!loadedParams.Contains(key))
                {
                    if (value is BymlIter subIter)
                    {
                        if (subIter.Type == BymlDataType.Array)
                            dict.Add(key, Helpers.Placement.ConvertToList(subIter));
                        else
                            dict.Add(key, Helpers.Placement.ConvertToDict(subIter));
                    }
                    else
                    {
                        dict.Add(key, value);
                    }
                }
            }

            return dict;
        }

        private static BymlContainer SaveValues(object obj)
        {
            BymlHash serializedNode = new();

            var properties = obj.GetType().GetProperties();

            foreach (var property in properties)
            {
                var value = property.GetValue(obj);

                if (property.PropertyType == typeof(Vector3))
                    serializedNode.Add(property.Name, Helpers.Placement.SaveVector((Vector3)value));
                else if (property.PropertyType == typeof(PlacementUnitConfig))
                    serializedNode.Add(property.Name, SaveValues(value));
                else if (property.Name == "Comment") // this is the last param in the node, so terminate the properties loop
                {
                    serializedNode[property.Name.ToLower()] = value;
                    break;
                }
                else if(property.Name == "Links" && obj is PlacementInfo info)
                    serializedNode.Add(property.Name, info.SerializeLinks());
                else 
                    serializedNode[property.Name] = value;
            }

            return serializedNode;
        }

        private static Dictionary<string, dynamic> SaveVector(Vector3 vec)
        {
            return new Dictionary<string, dynamic>() {
                {"X", vec.X },
                {"Y", vec.Y },
                {"Z", vec.Z }
            };
        }

        private static void LoadValues(object obj, BymlIter iter)
        {
            var properties = obj.GetType().GetProperties();

            foreach (var property in properties)
            {
                if (property.Name != "Comment" && !iter.ContainsKey(property.Name))
                {
                    Console.WriteLine($"Property {property.Name} was not found in Placement.");
                    continue;
                }

                if (!iter.TryGetValue(property.Name, out object value))
                {
                    iter.TryGetValue(property.Name.ToLower(), out value);

                    if (obj is PlacementInfo info)
                        info.loadedParams.Add(property.Name.ToLower());
                }else
                {
                    if (obj is PlacementInfo info)
                        info.loadedParams.Add(property.Name);
                }

                if (property.PropertyType == typeof(Vector3))
                    property.SetValue(obj, Helpers.Placement.LoadVector((BymlIter)value));
                else if (property.PropertyType == typeof(PlacementUnitConfig))
                    LoadValues(property.GetValue(obj), (BymlIter)value);
                else if (property.Name == "Comment") // this is the last param in the node, so terminate the properties loop
                {
                    property.SetValue(obj, value); // dumb
                    break;
                }
                else if(value is BymlIter subIter)
                {
                    if(obj is PlacementInfo info && property.Name == "Links")
                        info.DeserializeLinks(subIter);
                    else
                        property.SetValue(obj, Helpers.Placement.ConvertToDict(subIter));
                }
                else
                    property.SetValue(obj, value);
            }
        }

        private static void LoadValues(object obj, Dictionary<string,dynamic> node)
        {
            var properties = obj.GetType().GetProperties();

            foreach (var property in properties)
            {

                if (property.Name != "Comment" && !node.ContainsKey(property.Name))
                {
                    Console.WriteLine($"Property {property.Name} was not found in Placement.");
                    continue;
                }

                if (property.PropertyType == typeof(Vector3))
                    property.SetValue(obj, Helpers.Placement.LoadVector(node, property.Name));
                else if (property.PropertyType == typeof(PlacementUnitConfig))
                    LoadValues(property.GetValue(obj), node[property.Name]);
                else if (property.Name == "Comment") // this is the last param in the node, so terminate the properties loop
                {

                    if (!node.ContainsKey(property.Name.ToLower()))
                    {
                        Console.WriteLine($"Property {property.Name} was not found in Placement.");
                        break;
                    }

                    property.SetValue(obj, node[property.Name.ToLower()]); // dumb
                    node.Remove(property.Name.ToLower());
                    break;
                }
                else
                    property.SetValue(obj, node[property.Name]);

                node.Remove(property.Name);
            }
        }
        
        public void DeserializeByml(BymlIter rootNode)
        {
            LoadValues(this, rootNode);

            ActorParams = GetActorParams(rootNode);

            isUseLinks = Links?.Count > 0;
        }

        public BymlContainer SerializeByml()
        {
            var serializedDefaults = SaveValues(this);

            foreach (var kvp in ActorParams)
                serializedDefaults[kvp.Key] = Helpers.Placement.SaveObject(kvp.Value);

            return serializedDefaults;
        }

        private void DeserializeLinks(BymlIter rootNode)
        {
            foreach ((var linkName, var placementListIter) in rootNode.As<BymlIter>())
            {
                if (!Links.TryGetValue(linkName, out var placementList))
                    Links.Add(linkName, placementList = new List<PlacementInfo>());

                foreach (var subPlacementIter in placementListIter.AsArray<BymlIter>())
                    placementList.Add(new PlacementInfo(subPlacementIter));
            }
        }

        private BymlContainer SerializeLinks()
        {
            BymlHash linkHash = new BymlHash();

            foreach ((var linkName, var placementList) in Links)
            {
                var placementListIter = new BymlArray();
                linkHash.Add(linkName, placementListIter);

                foreach (var subPlacement in placementList)
                    placementListIter.Add(subPlacement.SerializeByml());
            }

            return linkHash;
        }

        public static bool operator ==(PlacementInfo obj1, PlacementInfo obj2) => ReferenceEquals(obj1, obj2) || (!ReferenceEquals(obj1, null) && !ReferenceEquals(obj2, null) && obj1.Equals(obj2));
        public static bool operator !=(PlacementInfo obj1, PlacementInfo obj2) => !(obj1 == obj2);
        public static bool operator ==(PlacementInfo obj1, PlacementId obj2) => !ReferenceEquals(obj1, null) && !ReferenceEquals(obj2, null) && obj1.Equals(obj2);
        public static bool operator !=(PlacementInfo obj1, PlacementId obj2) => !(obj1 == obj2);
        public bool Equals(PlacementInfo other) => Id == other.Id && UnitConfigName == other.UnitConfigName && LayerConfigName == other.LayerConfigName;
        public bool Equals(PlacementId other) => Id == other.Id && UnitConfigName == other.UnitConfigName && LayerConfigName == other.LayerConfigName;
        public override bool Equals(object obj)
        {
            if (obj is PlacementId id)
                return Equals(id);
            else
                return Equals(obj as PlacementInfo);
        }
        public override int GetHashCode() => HashCode.Combine(Id, UnitConfigName, LayerConfigName);
    }
}
