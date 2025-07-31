using ImGuiNET;
using MapStudio.UI;
using RedStarLibrary.GameTypes;
using RedStarLibrary.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace RedStarLibrary.UI
{
    public class AddObjectMenu
    {
        private string curSelectedCategory = "";
        private string curSelectedClass = "";
        private string curModelName = "";
        private string searchModelText = "";
        private string searchObjText = "";

        private List<string> categoryList;
        private List<string> classList;
        private List<string> filteredClassList;
        private Dictionary<string, dynamic> curParams;
        private Dictionary<string, List<string>> linkActorClasses;

        private ObjectDatabaseEntry curEntry;
        private ObjectDatabaseEntry linkActorEntry;

        private bool doesModelExist = false;
        private bool isForceCreate = false;
        private bool isShowLinkOnly = false;

        public AddObjectMenu() { }

        public void Init()
        {
            linkActorClasses = new();

            UpdateCategoryList();

            UpdateClassList();
        }

        public void UpdateCategoryList()
        {
            categoryList = ActorDataBase.GetAllCategories();
            categoryList.Sort();

            curSelectedCategory = "Object";
        }

        public void SetLinkDataEntry(LiveActor liveActor)
        {
            var objCategory = liveActor.Placement.UnitConfig.GenerateCategory;
            objCategory = objCategory.Substring(0, objCategory.Length - 4); // remove "List" from category string

            linkActorEntry = ActorDataBase.GetObjectFromDatabase(liveActor.Placement.ClassName, objCategory);

            if(linkActorEntry == null)
            {
                isShowLinkOnly = false;
                UpdateCategoryList();
            }
            else
            {
                isShowLinkOnly = true;
                UpdateLinkCategoryList();
            }

            UpdateClassList();
        }

        public void UpdateLinkCategoryList()
        {
            categoryList = [linkActorEntry.ActorCategory];
            linkActorClasses.Clear();

            foreach (var linkObjClass in linkActorEntry.LinkActors)
            {
                bool isInSameCategory = false;

                var linkObjEntry = ActorDataBase.GetObjectFromDatabase(linkObjClass, linkActorEntry.ActorCategory); // look for link obj with the same category as parent first
                if (linkObjEntry == null)
                    linkObjEntry = ActorDataBase.GetObjectFromDatabase(linkObjClass); // if above fails, just do a general search for the class
                else
                    isInSameCategory = true;  // if we found the actor with the same category as parent, no need to add its category to the list

                if (linkObjEntry == null)
                    continue;

                if (!linkActorClasses.TryGetValue(linkObjEntry.ActorCategory, out List<string> linkClassList))
                    linkActorClasses.Add(linkObjEntry.ActorCategory, linkClassList = [linkObjEntry.ClassName]);
                else
                    linkClassList.Add(linkObjEntry.ClassName);

                if (!isInSameCategory && !categoryList.Contains(linkObjEntry.ActorCategory))
                    categoryList.Add(linkObjEntry.ActorCategory);
            }

            curSelectedCategory = categoryList.FirstOrDefault();
        }

        public void Draw()
        {
            if(linkActorEntry != null && ImGui.Checkbox("Filter Objects to Links only", ref isShowLinkOnly))
            {
                if (isShowLinkOnly)
                    UpdateLinkCategoryList();
                else
                    UpdateCategoryList();

                UpdateClassList();
            }

            if (ImGui.CollapsingHeader($"Object Entry Selection" + (!string.IsNullOrWhiteSpace(curSelectedClass) ? $" (Selected: {curSelectedClass})" : ""), ImGuiTreeNodeFlags.DefaultOpen))
            {
                StudioUIHelper.DrawSelectDropdown("Object Category", ref curSelectedCategory, categoryList, UpdateClassList);

                DrawClassSearchBar();

                if (ImGui.BeginChild("ObjectClassList", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 100), true))
                {
                    foreach (var objClass in filteredClassList)
                    {
                        if (ImGui.Selectable(objClass, curSelectedClass == objClass))
                        {
                            curSelectedClass = objClass;
                            UpdateEntry();
                        }
                    }

                    ImGui.EndChild();
                }
            }

            if (curEntry != null)
                DrawCurrentEntry();
            else
            {
                if (!string.IsNullOrWhiteSpace(searchObjText))
                    ImGui.Checkbox("Force Create Actor", ref isForceCreate);
                else
                    isForceCreate = false;
            }

            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 212);
            ImGui.SetCursorPosY(ImGui.GetWindowHeight() - 35);

            bool cancel = ImGui.Button("Cancel", new Vector2(100, 23)); ImGui.SameLine();
            bool applied = ImGui.Button("Ok", new Vector2(100, 23)) && (curEntry != null || isForceCreate);

            if (cancel)
                DialogHandler.ClosePopup(false);
            if (applied)
                DialogHandler.ClosePopup(true);
        }

        public PlacementInfo GetPlacementInfo()
        {
            PlacementInfo info;

            string assetName = string.IsNullOrWhiteSpace(curModelName) ? curSelectedClass : curModelName;

            if (curEntry != null)
            {
                info = new PlacementInfo(curEntry, assetName);
                info.ActorParams = Helpers.Placement.CopyNode(curParams);
            }
            else if (isForceCreate)
                info = new PlacementInfo(searchObjText, assetName, curSelectedCategory);
            else
                throw new Exception("Unable to create PlacementInfo with supplied info.");

            linkActorEntry = null;

            return info;
        }

        private void UpdateClassList()
        {
            if(isShowLinkOnly && linkActorEntry != null && linkActorEntry.LinkActors.Any())
                classList = linkActorClasses[curSelectedCategory];
            else
                classList = ActorDataBase.GetClassNamesByCategory(curSelectedCategory);

            filteredClassList = classList;

            curEntry = null;
            curParams = null;
            curModelName = string.Empty;
            curSelectedClass = string.Empty;
        }

        private void UpdateEntry()
        {
            curEntry = ActorDataBase.GetObjectFromDatabase(curSelectedClass, curSelectedCategory); 
            curParams = Helpers.Placement.GetActorParamsFromDatabaseEntry(curEntry);
            curModelName = string.Empty;
        }

        private void DrawClassSearchBar()
        {
            if (ImGui.InputText("Search Object Class##addobj_search_class_box", ref searchObjText, 200))
            {
                if (!string.IsNullOrWhiteSpace(searchObjText))
                    filteredClassList = classList.Where(e => e.IndexOf(searchObjText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                else
                    UpdateClassList();
            }
        }

        private List<string> DrawModelSearchBar(List<string> models)
        {
            if(ImGui.InputText("Search/Set Model(s)##addobj_search_model_box", ref searchModelText, 200))
                doesModelExist = File.Exists(ResourceManager.FindResourcePath(Path.Combine("ObjectData", $"{searchModelText}.szs")));

            if (!string.IsNullOrWhiteSpace(searchModelText))
                return models.Where(e => e.IndexOf(searchModelText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            else
                return models;
        }

        private void DrawCurrentEntry()
        {
            if (curEntry.Models.Any())
            {
                if (ImGui.CollapsingHeader("Object Model(s)" + (!string.IsNullOrWhiteSpace(curModelName) ? $" (Selected: {curModelName})" : ""), ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var listedModels = DrawModelSearchBar(curEntry.Models.ToList());

                    if (listedModels.Any())
                    {
                        if (ImGui.BeginChild("ModelList", new Vector2(ImGui.GetContentRegionAvail().X, 150), true))
                        {
                            RenderObjectList(listedModels);
                            ImGui.EndChild();
                        }
                    }
                    else
                    {
                        if(doesModelExist)
                            curModelName = searchModelText;
                        else if (!string.IsNullOrWhiteSpace(searchModelText))
                            ImGui.Text("Warning: Model does not exist in game/current working directory!");
                    }
                }
            }
            else if(curEntry.ActorCategory == "Area")
            {
                StudioUIHelper.DrawSelectDropdown("Area Model", ref curModelName, ActorDataBase.AreaModelNames);
            }
            else
            {
                if (ImGui.CollapsingHeader("Object Model", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if(ImGui.InputText("Set Actor Model##addobj_set_model_box", ref curModelName, 200) && !string.IsNullOrWhiteSpace(curModelName))
                        doesModelExist = File.Exists(ResourceManager.FindResourcePath(Path.Combine("ObjectData", $"{curModelName}.szs")));

                    if(!string.IsNullOrWhiteSpace(curModelName) && !doesModelExist)
                        ImGui.Text("Warning: Model does not exist in game/current working directory!");
                }
                    
            }

            if (curParams.Any())
            {
                if (ImGui.CollapsingHeader("Object Properties", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGui.BeginChild("PropertiesList", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 320)))
                    {
                        PropertyDrawer.DrawProperties(curParams);
                        ImGui.EndChild();
                    }
                }
            }
        }

        public void RenderObjectList(List<string> modelList)
        {
            var itemHeight = 40;

            //Setup list spacing
            var spacing = ImGui.GetStyle().ItemSpacing;
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(spacing.X, 0));

            foreach (var modelName in modelList)
            {
                string fullPath;
                if (curEntry.Models.Count > 1)
                    fullPath = Path.Combine(PlacementFileEditor.ThumbnailPath, curEntry.ActorCategory, curEntry.ClassName, $"{modelName}.png");
                else
                    fullPath = Path.Combine(PlacementFileEditor.ThumbnailPath, curEntry.ActorCategory, $"{modelName}.png");

                var icon = IconManager.GetTextureIcon("Node");
                if (IconManager.HasIcon(fullPath))
                    icon = IconManager.GetTextureIcon(fullPath);

                //Load the icon onto the list
                ImGui.Image((IntPtr)icon, new Vector2(itemHeight, itemHeight)); ImGui.SameLine();
                ImGuiHelper.IncrementCursorPosX(3);

                Vector2 itemSize = new Vector2(ImGui.GetWindowWidth(), itemHeight);

                //Selection handling
                bool isSelected = curModelName == modelName;
                ImGui.AlignTextToFramePadding();
                bool select = ImGui.Selectable(modelName, isSelected, ImGuiSelectableFlags.None, itemSize);
                bool hovered = ImGui.IsItemHovered();

                if (select)
                    curModelName = modelName;
            }

            ImGui.PopStyleVar();
        }
    }
}
