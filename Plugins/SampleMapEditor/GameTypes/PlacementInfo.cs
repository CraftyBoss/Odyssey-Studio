using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CafeLibrary;
using ByamlExt.Byaml;
using OpenTK;
using RedStarLibrary.MapData;
using Toolbox.Core;
using HakoniwaByml.Iter;
using HakoniwaByml.Writer;

namespace RedStarLibrary.GameTypes
{
    public class PlacementInfo : IEquatable<PlacementInfo>, Interfaces.IBymlSerializable
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
            public string GenerateCategory { get; set; }
            /// <summary>
            /// string used for constructing LiveActor using the game's ProjectActorFactory
            /// </summary>
            [BindGUI("Class Name", Category = "Unit Config")]
            public string ParameterConfigName { get; set; }
            /// <summary>
            /// suffix of stage file (used for saving placement info to the correct Map/Design/Sound file possibly?)
            /// </summary>
            public string PlacementTargetFile { get; set; }
        }

        // byaml dictionary data obtained from stage byml
        // public Dictionary<string, dynamic> actorNode;

        /// <summary>
        /// object ID used to differentiate actors
        /// </summary>
        [BindGUI("Object ID", Category = "Placement Info")]
        public string Id { get; set; }
        /// <summary>
        /// Bool used to describe whether or not placement info is the destination of a link
        /// </summary>
        [BindGUI("Is Link Destination", Category = "Placement Info")]
        public bool IsLinkDest { get; set; }
        /// <summary>
        /// metadata leftover from official level editor used to handle cross-scenario objects
        /// </summary>
        [BindGUI("Object Layer", Category = "Placement Info")]
        public string LayerConfigName { get; set; }
        /// <summary>
        /// List of all Linked objects used by actor, separated by list categories
        /// </summary>
        public Dictionary<string,dynamic> Links { get; set; }
        /// <summary>
        /// name of actor's model
        /// </summary>
        [BindGUI("Object Model", Category = "Placement Info")]
        public string ModelName { get; set; }
        /// <summary>
        /// name of stage this placement info is found in
        /// </summary>
        [BindGUI("Placement Stage", Category = "Placement Info")]
        public string PlacementFileName { get; set; }
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

        /// <summary>
        /// List of every link that the Placement links to.
        /// </summary>
        public Dictionary<string, List<PlacementInfo>> sourceLinks;

        /// <summary>
        /// List of every link that the Placement is a destination of.
        /// </summary>
        public Dictionary<string, List<PlacementInfo>> destLinks;

        public bool isActorLoaded = false;

        public bool isUseLinks = false;

        public bool isLinkedInfo = false; // determines whether or not the placement is an object created from a parent info

        private List<string> loadedParams;
        public PlacementInfo()
        {

            UnitConfig = new PlacementUnitConfig();

            loadedParams = new List<string>();

            ActorParams = new Dictionary<string, dynamic>();

            isUseLinks = false;

            sourceLinks = new Dictionary<string, List<PlacementInfo>>();

            destLinks = new Dictionary<string, List<PlacementInfo>>();
        }

        public PlacementInfo(BymlIter actorIter)
        {

            UnitConfig = new PlacementUnitConfig();

            loadedParams = new List<string>();

            DeserializeByml(actorIter);

            sourceLinks = new Dictionary<string, List<PlacementInfo>>();

            destLinks = new Dictionary<string, List<PlacementInfo>>();

        }

        public PlacementInfo(ObjectDatabaseEntry objEntry)
        {

            UnitConfig = new PlacementUnitConfig();

            ActorParams = new Dictionary<string, dynamic>();

            isUseLinks = false;

            sourceLinks = new Dictionary<string, List<PlacementInfo>>();

            destLinks = new Dictionary<string, List<PlacementInfo>>();

            // load params
            foreach (var param in objEntry.ActorParams)
            {

                dynamic value;
                if(param.Value.First().GetType() == typeof(long))
                {
                    value = (int)param.Value.First();
                }else
                {
                    value = param.Value.First();
                }

                ActorParams.Add(param.Key, value);
            }

            // load default placement info data

            ClassName = objEntry.ClassName;

            ObjectName = objEntry.ClassName;

            UnitConfig.PlacementTargetFile = objEntry.PlacementCategory;

            UnitConfig.GenerateCategory = objEntry.ActorCategory + "List";

            ModelName = objEntry.Models.FirstOrDefault();

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
        private static Dictionary<string,dynamic> SaveValues(object obj)
        {

            Dictionary<string, dynamic> serializedNode = new Dictionary<string, dynamic>();

            var properties = obj.GetType().GetProperties();

            foreach (var property in properties)
            {

                var value = property.GetValue(obj);

                //if (property.Name != "Comment" && value == null) 
                //    continue; // null values in the info most likely means the loader didnt find the value.

                if (property.PropertyType == typeof(Vector3))
                    serializedNode.Add(property.Name, SaveVector((Vector3)value));
                else if (property.PropertyType == typeof(PlacementUnitConfig))
                    serializedNode.Add(property.Name, SaveValues(value));
                else if (property.Name == "Comment") // this is the last param in the node, so terminate the properties loop
                {
                    serializedNode.Add(property.Name.ToLower(), value);
                    break;
                }
                else
                    serializedNode.Add(property.Name, value);
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
        public static bool operator ==(PlacementInfo obj1, PlacementInfo obj2)
        {
            if (ReferenceEquals(obj1, obj2))
                return true;
            if (ReferenceEquals(obj1, null))
                return false;
            if (ReferenceEquals(obj2, null))
                return false;
            return obj1.Equals(obj2);
        }
        public static bool operator !=(PlacementInfo obj1, PlacementInfo obj2) => !(obj1 == obj2);
        public bool Equals(PlacementInfo other)
        {
            return Id == other.Id && UnitConfigName == other.UnitConfigName && LayerConfigName == other.LayerConfigName;
        }
        public override bool Equals(object obj) => Equals(obj as PlacementInfo);
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, UnitConfigName, LayerConfigName);
        }

        public void DeserializeByml(BymlIter rootNode)
        {

            LoadValues(this, rootNode);

            ActorParams = GetActorParams(rootNode);

            if (Links.Count > 0)
            {
                isUseLinks = true;
            }

        }

        public BymlContainer SerializeByml()
        {
            Dictionary<string, dynamic> serializedDefaults = SaveValues(this);

            foreach (var kvp in ActorParams)
            {
                serializedDefaults.Add(kvp.Key, kvp.Value);
            }

            return Helpers.Placement.ConvertToHash(serializedDefaults);
        }
    }
}
