using CafeLibrary.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RedStarLibrary.Extensions;
using RedStarLibrary.GameTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;

namespace RedStarLibrary
{
    [JsonObject]
    public class ObjectDatabaseEntry
    {
        public class ParamEntry
        {
            /// <summary>
            /// Determines if the parameter is required to create the actor in game (will need to be determined manually)
            /// </summary>
            [JsonProperty]
            public bool Required = false;
            /// <summary>
            /// List of all values found for the parameter in every stage (can be useful for some parameters with fixed values)
            /// </summary>
            [JsonProperty]
            public HashSet<dynamic> FoundValues = new HashSet<dynamic>();
        }

        /// <summary>
        /// Internal name for the LiveActor class the actor uses in game.
        /// The games ActorFactory references this string when constructing the LiveActor for the scene.
        /// </summary>
        [JsonProperty]
        public string ClassName;
        /// <summary>
        /// Placement type that the actor is supposed to be placed in (Map, Design, or Sound)
        /// </summary>
        [JsonProperty]
        public string PlacementCategory;
        /// <summary>
        /// Object Category that the actor is used in (usually something like ObjectList, AreaList, etc)
        /// </summary>
        [JsonProperty]
        public string ActorCategory;
        /// <summary>
        /// List of unique models used by the actor.
        /// </summary>
        [JsonProperty]
        public HashSet<string> Models;
        /// <summary>
        /// dictionary containing a unique list of every value found for the parameters the actor can have
        /// </summary>
        [JsonProperty]
        public Dictionary<string, ParamEntry> ActorParams;
    }

    public class ActorDataBase
    {
        public static List<ObjectDatabaseEntry> ObjDatabase { get; private set; } = null; // game has 570 actors in 1.0, this should have most, if not all, of them

        private static HashSet<string> UnusedObjs = new HashSet<string>();

        private static readonly List<string> ParamsUseEmptyString = new List<string>()
        {
            "ChangeStageId",
            "ChangeStageName"
        };

        public static List<ObjectDatabaseEntry> GetDataBase()
        {
            if(ObjDatabase == null)
                LoadDatabase();

            return ObjDatabase;
        }

        public static ObjectDatabaseEntry GetObjectFromDatabase(string databaseName, string category = null)
        {
            if(ObjDatabase != null)
            {
                if(string.IsNullOrEmpty(databaseName))
                    return ObjDatabase.FirstOrDefault(e => e.ClassName.Equals(databaseName) || e.Models.Contains(databaseName));
                else
                    return ObjDatabase.FirstOrDefault(e => e.ActorCategory == category && (e.ClassName.Equals(databaseName) || e.Models.Contains(databaseName)));
            }
            else 
                return null;
        }

        public static List<string> GetAllLoadableModels()
        {
            if (ObjDatabase == null)
                LoadDatabase();

            HashSet<string> models = new HashSet<string>();

            foreach (var entry in ObjDatabase)
                models.Add(entry.Models.FirstOrDefault());
            //foreach (var modelString in entry.Models)


            return models.ToList();
        }

        public static void LoadDatabase()
        {
            if (ObjDatabase != null) 
                return;

            ReloadDataBase();

            // OrganizeThumbnails();
        }

        public static void ReloadDataBase()
        {
            var dictDatabase = JsonConvert.DeserializeObject<List<ObjectDatabaseEntry>>(File.ReadAllText("ObjectDatabase.json"));

            if (dictDatabase != null)
                ObjDatabase = dictDatabase;

            // TEMP: JsonConvert parses integer values as int64, so we need to iterate through the entire database and fix those values
            foreach (var entry in ObjDatabase)
            {
                foreach (var param in entry.ActorParams)
                {
                    for (var i = 0; i < param.Value.FoundValues.Count; i++)
                    {
                        var val = param.Value.FoundValues.ElementAt(i);

                        if (val is long longVal)
                        {
                            param.Value.FoundValues.Remove(val);
                            param.Value.FoundValues.Add(Convert.ToInt32(longVal));
                        }else if(val is double doubleVal)
                        {
                            param.Value.FoundValues.Remove(val);
                            param.Value.FoundValues.Add((float)doubleVal);
                        }
                    }
                }
            }
        }

        private static void OrganizeThumbnails()
        {

            var rootDir = $"{Runtime.ExecutableDir}\\Lib\\Images\\ActorThumbnails\\";

            HashSet<string> removedFiles = new HashSet<string>();

            foreach (var entry in ObjDatabase)
            {
                if(entry.Models.Count == 0)
                {
                    var prevPath = rootDir + $"{entry.ClassName}.png";

                    var newPath = rootDir + $"{entry.ActorCategory}\\{entry.ClassName}.png";

                    if (File.Exists(prevPath))
                    {
                        Console.WriteLine($"Copying {entry.ClassName} to Path: " + newPath);

                        Directory.CreateDirectory(Path.GetDirectoryName(newPath));

                        if(!File.Exists(newPath))
                            File.Copy(prevPath, newPath);

                        removedFiles.Add(prevPath);
                    }
                }
                else
                {
                    foreach (var modelName in entry.Models)
                    {
                        var newPath = "";

                        if (entry.Models.Count > 1)
                            newPath = rootDir + $"{entry.ActorCategory}\\{entry.ClassName}\\{modelName}.png";
                        else
                            newPath = rootDir + $"{entry.ActorCategory}\\{modelName}.png";

                        var prevPath = rootDir + $"{modelName}.png";

                        if (File.Exists(prevPath))
                        {
                            Console.WriteLine($"Copying {entry.ClassName} to Path: " + newPath);

                            Directory.CreateDirectory(Path.GetDirectoryName(newPath));

                            if (!File.Exists(newPath))
                                File.Copy(prevPath, newPath);

                            removedFiles.Add(prevPath);
                        }
                    }
                }
            }

            foreach (var path in removedFiles)
            {
                if(File.Exists(path))
                {
                    Console.WriteLine("Deleting File at Path: " + path);
                    File.Delete(path);
                }
            }

        }

        public static void RegisterActorsToDatabase(List<PlacementInfo> placementList)
        {
            foreach (var info in placementList)
            {
                var entry = ObjDatabase.Find(e => e.ClassName == info.ClassName);

                if(entry == null)
                {
                    string actorCategory = info.UnitConfig.GenerateCategory;

                    entry = new ObjectDatabaseEntry()
                    {
                        ClassName = info.ClassName,
                        PlacementCategory = info.UnitConfig.PlacementTargetFile,
                        ActorCategory = actorCategory.Remove(actorCategory.IndexOf("List"), 4),
                        Models = new HashSet<string>(),
                        ActorParams = new()
                    };

                    var copy = Helpers.Placement.CopyNode(info.ActorParams);

                    foreach (var kvp in copy)
                    {
                        if (kvp.Key == "SrcUnitLayerList")
                            continue;

                        if(kvp.Value is float)
                            entry.ActorParams.Add(kvp.Key, new ObjectDatabaseEntry.ParamEntry() { FoundValues = new HashSet<dynamic>() { 0.0f } });
                        else if(ParamsUseEmptyString.Contains(kvp.Key))
                            entry.ActorParams.Add(kvp.Key, new ObjectDatabaseEntry.ParamEntry() { FoundValues = new HashSet<dynamic>() { "" } });
                        else
                            entry.ActorParams.Add(kvp.Key, new ObjectDatabaseEntry.ParamEntry() { FoundValues = new HashSet<dynamic>() { kvp.Value } });
                    }

                    Console.WriteLine($"Added {info.ClassName} to Database.");

                    ObjDatabase.Add(entry);
                }else
                {
                    foreach (var param in info.ActorParams)
                    {

                        if (param.Key == "SrcUnitLayerList") 
                            continue;

                        if (!entry.ActorParams.ContainsKey(param.Key))
                        {
                            if (param.Value is float)
                                entry.ActorParams.Add(param.Key, new ObjectDatabaseEntry.ParamEntry() { FoundValues = new HashSet<dynamic>() { 0.0f } });
                            else if (ParamsUseEmptyString.Contains(param.Key))
                                entry.ActorParams.Add(param.Key, new ObjectDatabaseEntry.ParamEntry() { FoundValues = new HashSet<dynamic>() { "" } });
                            else
                                entry.ActorParams.Add(param.Key, new ObjectDatabaseEntry.ParamEntry() { FoundValues = new HashSet<dynamic>() { param.Value } });
                        }
                        else
                        {
                            if (param.Value is float)
                                entry.ActorParams[param.Key].FoundValues.Add(0.0f);
                            else if (ParamsUseEmptyString.Contains(param.Key))
                                entry.ActorParams[param.Key].FoundValues.Add("");
                            else
                                entry.ActorParams[param.Key].FoundValues.Add(param.Value);
                        }
                    }
                }

                string modelName = info.ModelName != null ? info.ModelName : info.UnitConfigName;

                if(modelName != null)
                {
                    string modelPath = ResourceManager.FindResourcePath($"ObjectData\\{modelName}.szs");

                    if (File.Exists(modelPath))
                    {
                        var modelARC = ResourceManager.FindOrLoadSARC(modelPath);

                        var modelStream = modelARC.GetModelStream(Path.GetFileNameWithoutExtension(modelPath));

                        if (modelStream != null)
                        {
                            if (entry.Models.Add(modelName))
                                Console.WriteLine($"Added the Model {modelName} to {entry.ClassName}.");
                        }
                    }
                }else
                {
                    if(UnusedObjs.Add(info.ClassName))
                        Console.WriteLine($"Added {info.ClassName} to list of Unused Objects.");
                }
            }
        }

        public static void SerializeDatabase()
        {
            ObjDatabase = ObjDatabase.OrderBy(e => e.ClassName).ToList();

            Helpers.JsonHelper.WriteToJSON(ObjDatabase, "ObjectDatabase.json");

        }

    }
}
