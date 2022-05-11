using System;
using System.IO;
using Toolbox.Core;
using MapStudio.UI;
using OpenTK;
using GLFrameworkEngine;
using CafeLibrary;
using ByamlExt.Byaml;
using System.Collections.Generic;
using RedStarLibrary.GameTypes;
using RedStarLibrary.Rendering;

namespace RedStarLibrary
{
    /// <summary>
    /// Represents a class used for loading files into the editor.
    /// IFileFormat determines what files to use. FileEditor is used to store all the editor information.
    /// </summary>
    public class EditorLoader : FileEditor, IFileFormat
    {
        /// <summary>
        /// The description of the file extension of the plugin.
        /// </summary>
        public string[] Description => new string[] { "Map Data" };

        /// <summary>
        /// The extension of the plugin. This should match whatever file you plan to open.
        /// </summary>
        public string[] Extension => new string[] { "*.szs" };

        /// <summary>
        /// Determines if the plugin can save or not.
        /// </summary>
        public bool CanSave { get; set; } = true;

        /// <summary>
        /// File info of the loaded file format.
        /// </summary>
        public File_Info FileInfo { get; set; }

        /// <summary>
        /// All Placement Info used in the loaded Stage
        /// </summary>
        public Dictionary<string, Dictionary<string, List<PlacementInfo>>> MapPlacementList;

        public Dictionary<string, List<ActorList>> MapActorList;

        /// <summary>
        /// SARC containing all data used in the map
        /// </summary>
        private SARC mapArc;

        private string mapName;

        /// <summary>
        /// Determines when to use the map editor from a given file.
        /// You can check from file extension or check the data inside the file stream.
        /// The file stream is always decompressed if the given file has a supported ICompressionFormat like Yaz0.
        /// </summary>
        public bool Identify(File_Info fileInfo, Stream stream)
        {
            return fileInfo.Extension == ".szs";
        }

        const bool USE_GAME_SHADERS = false;

        /// <summary>
        /// Loads the given file data from a stream.
        /// </summary>
        public void Load(Stream stream)
        {
            //Set the game shader
            if (USE_GAME_SHADERS)
                CafeLibrary.Rendering.BfresLoader.AddShaderType(typeof(SMORenderer));

            mapArc = new SARC();

            mapArc.Load(stream);

            ArchiveFileInfo mapData = mapArc.files.Find(e => e.FileName.Contains("StageMap.byml") || e.FileName.Contains("StageDesign.byml") || e.FileName.Contains("StageSound.byml"));

            if(mapData != null)
            {

                mapName = mapData.FileName;

                // Dict of Scenarios containing List of Dicts that contain Categories of PlacementInfo
                MapPlacementList = new Dictionary<string, Dictionary<string, List<PlacementInfo>>>();

                BymlFileData mapByml = ByamlFile.LoadN(mapData.FileData, false);

                int scenarioNo = 0;

                foreach (Dictionary<string, dynamic> scenarioNode in mapByml.RootNode as List<dynamic>)
                {

                    Dictionary<string, List<PlacementInfo>> scenarioList = new Dictionary<string, List<PlacementInfo>>();

                    string scenarioName = $"Scenario{scenarioNo}";

                    foreach (var actorListNode in scenarioNode)
                    {

                        if(!scenarioList.ContainsKey(actorListNode.Key))
                        {
                            scenarioList.Add(actorListNode.Key, new List<PlacementInfo>());
                        }

                        List<PlacementInfo> actors = scenarioList[actorListNode.Key];

                        foreach (Dictionary<string, dynamic> actorNode in actorListNode.Value)
                        {
                            PlacementInfo actorInfo = new PlacementInfo(actorNode);

                            if (actorInfo.isUseLinks)
                            {
                                CreateAllActors(scenarioList, actors, actorInfo);
                            }
                            else
                            {
                                actors.Add(actorInfo);
                            }
                        }
                    }

                    MapPlacementList.Add(scenarioName, scenarioList);

                    scenarioNo++;
                }

                //For this example I will show loading 3D objects into the scene
                MapScene scene = new MapScene();
                scene.Setup(this);

            }
            else
            {
                throw new FileLoadException("Unable to Load Archive!");
            }
        }

        /// <summary>
        /// Saves the given file data to a stream.
        /// </summary>
        public void Save(Stream stream)
        {

            BymlFileData mapByml = new BymlFileData()
            {
                byteOrder = Syroot.BinaryData.ByteOrder.LittleEndian,
                SupportPaths = false,
                Version = 3
            };
            // List<Dictionary<string,List<Dictionary<string,dynamic>>>>
            List<dynamic> serializedDict = new List<dynamic>();

            ArchiveFileInfo mapData = mapArc.files.Find(e => e.FileName.Contains("StageMap.byml") || e.FileName.Contains("StageDesign.byml") || e.FileName.Contains("StageSound.byml"));

            BymlFileData origMapByml = ByamlFile.LoadN(mapData.FileData, false);

            int scenarioNo = 0;

            foreach (var mapScenario in MapPlacementList)
            {

                Dictionary<string, dynamic> scenarioDict = new Dictionary<string, dynamic>();

                List<ActorList> actorCategories;

                MapActorList.TryGetValue($"Scenario{scenarioNo}", out actorCategories);

                if (actorCategories != null)
                {
                    actorCategories.ForEach(e => e.UpdateAllActorPlacement());
                }

                foreach (var mapActorLists in mapScenario.Value)
                {

                    if (mapActorLists.Key == "LinkedObjs") continue;

                    List<dynamic> actorList = new List<dynamic>();

                    foreach (var actorPlacementInfo in mapActorLists.Value)
                    {

                        actorPlacementInfo.SaveTransform();

                        actorList.Add(actorPlacementInfo.actorNode);
                    }

                    scenarioDict.Add(mapActorLists.Key, actorList);
                }

                serializedDict.Add(scenarioDict);

                scenarioNo++;
            }

            mapByml.RootNode = serializedDict;

            mapArc.SetFileData(mapName, new MemoryStream(ByamlFile.SaveN(mapByml)));

            mapArc.Save(stream);
        }

        //Extra overrides for FileEditor you can use for custom UI

        /// <summary>
        /// Draws the viewport menu bar usable for custom tools.
        /// </summary>
        public override void DrawViewportMenuBar()
        {

        }

        /// <summary>
        /// When an asset item from the asset windows gets dropped into the editor.
        /// You can configure your own asset category from the asset window and make custom asset items to drop into.
        /// </summary>
        public override void AssetViewportDrop(AssetItem item, Vector2 screenPosition)
        {
            //viewport context
            var context = GLContext.ActiveContext;

            //Screen coords can be converted into 3D space
            //By default it will spawn in the mouse position at a distance
            Vector3 position = context.ScreenToWorld(screenPosition.X, screenPosition.Y, 100);
            //Collision dropping can be used to drop these assets to the ground from CollisionCaster
            if (context.EnableDropToCollision)
            {
                Quaternion rot = Quaternion.Identity;
                CollisionDetection.SetObjectToCollision(context, context.CollisionCaster, screenPosition, ref position, ref rot);
            }
        }

        /// <summary>
        /// Checks for dropped files to use for the editor.
        /// If the value is true, the file will not be loaded as an editor if supported.
        /// </summary>
        public override bool OnFileDrop(string filePath)
        {
            return false;
        }

        private void CreateAllActors(Dictionary<string, List<PlacementInfo>> scenarioActorLists, List<PlacementInfo> curActorList, PlacementInfo actorInfo)
        {

            if(!scenarioActorLists.ContainsKey("LinkedObjs")) scenarioActorLists.Add("LinkedObjs", new List<PlacementInfo>());

            List<PlacementInfo> linkedObjList = scenarioActorLists["LinkedObjs"];

            foreach (var linkList in actorInfo.Links)
            {
                foreach (Dictionary<string, dynamic> objNode in linkList.Value)
                {

                    if (!linkedObjList.Exists(e => e.ObjID == objNode["Id"]))
                    {
                        PlacementInfo childActorPlacement = new PlacementInfo(objNode);

                        if (childActorPlacement.isUseLinks)
                        {
                            CreateAllActors(scenarioActorLists, linkedObjList, childActorPlacement); // recursively call function
                        }else
                        {
                            linkedObjList.Add(childActorPlacement);
                        }

                        linkedObjList.Add(childActorPlacement);

                    }
                }
            }

            curActorList.Add(actorInfo);
        }
    }
}
