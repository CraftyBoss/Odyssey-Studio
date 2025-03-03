using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using MapStudio.UI;
using System.Numerics;
using Newtonsoft.Json.Linq;

namespace RedStarLibrary
{
    public class PropertyDrawer
    {
        static bool isUpdating = false;

        public static void Draw(IDictionary<string, dynamic> values, PropertyChangedCallback callback = null)
        {
            IDictionary<string, dynamic> properties = null;
            if (values.ContainsKey("UnitConfig"))
                properties = (IDictionary<string, dynamic>)values["UnitConfig"];

            float width = ImGui.GetWindowWidth();

            if (ImGui.CollapsingHeader("Object Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button("Edit Properties", new System.Numerics.Vector2(width, 22)))
                    DialogHandler.Show("Property Window", () => PropertiesDialog(values), null);

                ImGui.Columns(2);
                LoadProperties(values, callback);
                ImGui.Columns(1);
            }

            if (properties != null && ImGui.CollapsingHeader("Unit Config", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button("Edit Unit Config", new System.Numerics.Vector2(width, 22)))
                    DialogHandler.Show("Unit Config Window", () => PropertiesDialog(properties), null);

                ImGui.Columns(2);
                LoadProperties(properties, callback);
                ImGui.Columns(1);
            }
        }

        public static void DrawProperties(IDictionary<string, dynamic> properties)
        {
            ImGui.Columns(2);
            LoadProperties(properties);
            ImGui.Columns(1);
        }

        static List<string> removedProperties = new List<string>();

        //A dialog to add/remove properties.
        static void PropertiesDialog(IDictionary<string, dynamic> properties) // TODO: add support for adding known properties
        {
            if (isUpdating)
                return;

            if (ImGui.CollapsingHeader("Add New Property", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PushItemWidth(100);
                if (ImGui.Combo("", ref selectedPropertyType, PropertyTypes, PropertyTypes.Length, 100))
                {

                }
                ImGui.PopItemWidth();

                ImGui.SameLine();

                ImGui.InputText($"##addpropname", ref addPropertyName, 0x100);
                ImGui.SameLine();

                ImGui.PushItemWidth(100);
                if (ImGui.Button("Add"))
                {
                    if (!string.IsNullOrEmpty(addPropertyName))
                    {
                        isUpdating = true;

                        //Remove the existing property if exist. User may want to update the data type.
                        if (properties.ContainsKey(addPropertyName))
                            properties.Remove(addPropertyName);
                        properties.Add(addPropertyName, CreateDefaultProperty());
                        //Resort the properties as they are alphabetically ordered
                        var ordered = properties.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
                        properties.Clear();
                        foreach (var pair in ordered)
                            properties.Add(pair.Key, pair.Value);

                        isUpdating = false;
                    }
                }
                ImGui.PopItemWidth();
            }
            if(ImGui.CollapsingHeader("Add Existing Property"))
            {
                ImGui.Text("To be Implemented.");
            }
            if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(3);
                foreach (var pair in properties)
                {
                    string name = pair.Key;

                    switch (name)
                    {
                        case "UnitConfig":
                            continue;
                        case "Links":
                            continue;
                        case "Translate":
                            continue;
                        case "Rotate":
                            continue;
                        case "Scale":
                            continue;
                    }

                    ImGui.PushItemWidth(ImGui.GetColumnWidth());
                    ImGui.InputText($"##name{name}", ref name, 0x100);
                    ImGui.PopItemWidth();

                    ImGui.NextColumn();

                    DrawPropertiesDynamic(properties, pair.Key, pair.Value);

                    ImGui.NextColumn();

                    ImGui.PushItemWidth(80);

                    if (ImGui.Button($"Remove##{pair.Key}"))
                        removedProperties.Add(pair.Key);

                    ImGui.PopItemWidth();

                    ImGui.NextColumn();
                }
                ImGui.Columns(1);

                foreach (var prop in removedProperties)
                    properties.Remove(prop);

                if (removedProperties.Count > 0)
                    removedProperties.Clear();
            }
        }

        static dynamic CreateDefaultProperty()
        {
            switch (PropertyTypes[selectedPropertyType])
            {
                case "Float": return 0.0f;
                case "Int": return 0;
                case "Uint": return 0u;
                case "String": return "";
                case "Double": return 0d;
                case "ULong": return 0UL;
                case "Long": return 0L;
                case "Bool": return false;
                case "Float3":
                    var dict = new Dictionary<string, dynamic>();
                    dict.Add("X", 0.0f);
                    dict.Add("Y", 0.0f);
                    dict.Add("Z", 0.0f);
                    return dict;
                case "<NULL>": return null;
            }
            return null;
        }

        static string addPropertyName = "";

        static int selectedPropertyType = 0;

        static string[] PropertyTypes = new string[]
        {
            "Float", "Int", "String", "Bool", "Float3", "Uint", "Double", "ULong", "Long", "<NULL>"
        };

        static readonly List<string> ExcludedProperties = new List<string>
        {
            "!Parameters", "Scale", "Translate", "Rotate"
        };

        public static void LoadPropertyUI(IDictionary<string, dynamic> properties, string category = "PROPERTIES")
        {
            if (ImGui.CollapsingHeader(TranslationSource.GetText(category), ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);
                LoadProperties(properties);
                ImGui.Columns(1);
            }
        }

        static void LoadProperties(IDictionary<string, dynamic> properties, PropertyChangedCallback callback = null)
        {
            foreach (var pair in properties.ToList())
            {
                //Skip lists, scale, rotate, etc properties as they are loaded in the UI in other places
                if (ExcludedProperties.Contains(pair.Key))
                    continue;

                if (pair.Value is IList<dynamic> || pair.Value is IDictionary<string, dynamic> || pair.Value == null)
                    continue;

                ImGui.Text(pair.Key);
                ImGui.NextColumn();

                DrawPropertiesDynamic(properties, pair.Key, pair.Value, callback);

                ImGui.NextColumn();
            }
        }

        static void DrawPropertiesDynamic(IDictionary<string, dynamic> properties, string key, dynamic value, PropertyChangedCallback callback = null)
        {
            float colwidth = ImGui.GetColumnWidth();
            float width = ImGui.GetWindowWidth();
            ImGui.SetColumnOffset(1, width * 0.5f);

            ImGui.PushItemWidth(colwidth);
            if (value != null)
            {
                Type type = value.GetType();

                //Check type and set property UI here
                if (type == typeof(float))
                    DrawFloat(properties, key, callback);
                else if (type == typeof(double))
                    DrawDouble(properties, key, callback);
                else if (type == typeof(int))
                    DrawInt(properties, key, callback);
                else if (type == typeof(uint))
                    DrawUint(properties, key, callback);
                else if (type == typeof(string))
                    DrawString(properties, key, callback);
                else if (type == typeof(bool))
                    DrawBool(properties, key, callback);
                else if (IsXYZ(value))
                    DrawXYZ(properties, key, callback);
                else
                    ImGui.Text(value.ToString());
            }
            else
            {
                if (key == "comment")
                    DrawNullString(properties, key, callback);
                else
                    ImGui.Text("<NULL>");
            }

            if (value is string)
            {
                ImGui.PushFont(ImGuiController.DefaultFontBold);
                string translation = GetStringTranslation(key, value);
                if (translation != null)
                    ImGui.Text(translation);
                ImGui.PopFont();
            }

            ImGui.PopItemWidth();
        }

        private static string GetStringTranslation(string key, string value)
        {
            switch (key)
            {
                case "UnitConfigName":
                    if (TranslationSource.HasKey($"ACTOR_NAME {value}"))
                        return TranslationSource.GetText($"ACTOR_NAME {value}");
                    return null;
            }
            return null;
        }

        public static bool IsXYZ(dynamic prop)
        {
            return prop is IDictionary<string, dynamic> &&
                ((IDictionary<string, dynamic>)prop).ContainsKey("X") &&
                ((IDictionary<string, dynamic>)prop).ContainsKey("Y") &&
                ((IDictionary<string, dynamic>)prop).ContainsKey("Z");
        }

        public static void DrawXYZ(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            dynamic values = (IDictionary<string, dynamic>)properties[key];
            var vec = new System.Numerics.Vector3(values["X"], values["Y"], values["Z"]);
            if (ImGui.DragFloat3($"##{key}", ref vec))
            {
                values["X"] = vec.X;
                values["Y"] = vec.Y;
                values["Z"] = vec.Z;
            }
        }

        public static void DrawString(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            string value = (string)properties[key];
            if (ImGui.InputText($"##{key}", ref value, 0x200))
            {
                properties[key] = (string)value;
                if (callback != null)
                    callback(key);
            }
        }

        public static void DrawNullString(IDictionary<string,dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            string value = "NULL";
            if (ImGui.InputText($"##{key}", ref value, 0x200))
            {
                properties[key] = (string)value;
                if (callback != null)
                    callback(key);
            }
        }

        public static void DrawDouble(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            double value = (double)properties[key];
            if (ImGui.InputDouble($"##{key}", ref value, 1, 1))
            {
                properties[key] = (double)value;
                if (callback != null)
                    callback(key);
            }
        }

        public static void DrawFloat(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            float value = (float)properties[key];
            if (ImGui.DragFloat($"##{key}", ref value, 1, 1))
            {
                properties[key] = (float)value;
                if (callback != null)
                    callback(key);
            }
        }

        public static void DrawInt(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            int value = (int)properties[key];
            if (ImGui.DragInt($"##{key}", ref value, 1, 1))
            {
                properties[key] = (int)value;
                if (callback != null)
                    callback(key);
            }
        }

        public static void DrawUint(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            int value = (int)((uint)properties[key]);
            if (ImGui.DragInt($"##{key}", ref value, 1, 1))
            {
                properties[key] = (uint)value;
                if (callback != null)
                    callback(key);
            }
        }

        public static void DrawBool(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            bool value = (bool)properties[key];
            if (ImGui.Checkbox($"##{key}", ref value))
            {
                properties[key] = (bool)value;
                if (callback != null)
                    callback(key);
            }
        }

        public delegate void PropertyChangedCallback(string key);
    }
}