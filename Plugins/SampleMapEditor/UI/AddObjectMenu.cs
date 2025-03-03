using ImGuiNET;
using MapStudio.UI;
using Newtonsoft.Json.Linq;
using RedStarLibrary.GameTypes;
using RedStarLibrary.MapData.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Toolbox.Core;
using UIFramework;

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

        private ObjectDatabaseEntry curEntry;
        private Dictionary<string, dynamic> curParams;

        private bool doesModelExist = false;

        public AddObjectMenu() 
        {
            categoryList = ActorDataBase.GetAllCategories();
            categoryList.Sort();

            curSelectedCategory = "Object";

            UpdateClassList();
        }

        public void Draw()
        {
            if(ImGui.CollapsingHeader($"Object Entry Selection" + (!string.IsNullOrWhiteSpace(curSelectedClass) ? $" (Selected: {curSelectedClass})" : ""), ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawSelectDropdown("Object Category", ref curSelectedCategory, categoryList, UpdateClassList);

                DrawClassSearchBar();
                //DrawSelectDropdown("Object Class", curSelectedClass, classList, UpdateEntry);

                if (ImGui.BeginChild("ObjectClassList", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 100), true))
                {
                    foreach (var objClass in classList)
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

            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 212);
            ImGui.SetCursorPosY(ImGui.GetWindowHeight() - 35);

            bool cancel = ImGui.Button("Cancel", new Vector2(100, 23)); ImGui.SameLine();
            bool applied = ImGui.Button("Ok", new Vector2(100, 23)) && curEntry != null;

            if (cancel)
                DialogHandler.ClosePopup(false);
            if (applied)
                DialogHandler.ClosePopup(true);
        }

        public PlacementInfo GetPlacementInfo()
        {
            PlacementInfo info = new PlacementInfo(curEntry, curModelName);

            info.ActorParams = Helpers.Placement.CopyNode(curParams);

            return info;
        }

        private void UpdateClassList()
        {
            classList = ActorDataBase.GetClassNamesByCategory(curSelectedCategory);
            curEntry = null;
            curModelName = string.Empty;
            curSelectedClass = string.Empty;
        }

        private void UpdateEntry()
        {
            curEntry = ActorDataBase.GetObjectFromDatabase(curSelectedClass, curSelectedCategory); 
            curParams = Helpers.Placement.GetActorParamsFromDatabaseEntry(curEntry);
            curModelName = string.Empty;
        }

        private void DrawSelectDropdown(string label, ref string preview, List<string> values, Action onSelect = null)
        {
            if (ImGui.BeginCombo(label, preview))
            {
                foreach (var objClass in values)
                {
                    if (ImGui.Selectable(objClass, preview == objClass))
                    {
                        preview = objClass;
                        onSelect?.Invoke();
                    }
                }

                ImGui.EndCombo();
            }
        }

        private void DrawClassSearchBar()
        {
            if (ImGui.InputText("Search Object Class##addobj_search_class_box", ref searchObjText, 200))
            {
                if (!string.IsNullOrWhiteSpace(searchObjText))
                    classList = ActorDataBase.GetClassNamesByCategory(curSelectedCategory).Where(e => e.IndexOf(searchObjText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                else
                    UpdateClassList();
            }
        }

        private List<string> DrawModelSearchBar(List<string> models)
        {
            if(ImGui.InputText("Search/Set Model(s)##addobj_search_model_box", ref searchModelText, 200))
                doesModelExist = File.Exists(ResourceManager.FindResourcePath($"ObjectData\\{searchModelText}.szs"));

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
                DrawSelectDropdown("Area Model", ref curModelName, ActorDataBase.AreaModelNames);
            }
            else
            {
                if (ImGui.CollapsingHeader("Object Model", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if(ImGui.InputText("Set Actor Model##addobj_set_model_box", ref curModelName, 200) && !string.IsNullOrWhiteSpace(curModelName))
                        doesModelExist = File.Exists(ResourceManager.FindResourcePath($"ObjectData\\{curModelName}.szs"));

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
                    fullPath = PlacementFileEditor.ThumbnailPath + $"\\{curEntry.ActorCategory}\\{curEntry.ClassName}\\{modelName}.png";
                else
                    fullPath = PlacementFileEditor.ThumbnailPath + $"\\{curEntry.ActorCategory}\\{modelName}.png";

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
