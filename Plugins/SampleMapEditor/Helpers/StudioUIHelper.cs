using ImGuiNET;
using System;
using System.Collections.Generic;

namespace RedStarLibrary.Helpers
{
    public partial class StudioUIHelper
    {
        /// <summary>
        /// Draws a Combo with the contents set to the provided Enumerable of values, and when a selection is made will optionally execute <paramref name="onSelect"/>.
        /// </summary>
        /// <param name="label">Combo-Box Label.</param>
        /// <param name="preview">Preview value (and currently set value) of the combo box.</param>
        /// <param name="values">List of potential values for the combo box to display.</param>
        /// <param name="onSelect">Action to execute when a selectable entry is activated.</param>
        public static void DrawSelectDropdown(string label, ref string preview, IEnumerable<string> values, Action onSelect = null)
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

        /// <summary>
        /// Sets the Dialog windows size and centers its position.
        /// Note: Only call this while within a Dialog func!
        /// </summary>
        /// <param name="size">Size for the window to be set to.</param>
        public static void SetDialogWindowSize(System.Numerics.Vector2 size)
        {
            var center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetWindowPos(center);

            ImGui.SetWindowSize(size);
        }
    }
}
