﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using BfresLibrary;
using MapStudio.UI;

namespace CafeLibrary
{
    public class UserDataInfoEditor
    {
        static List<UserData> Selected = new List<UserData>();

        static UserDataDialog ActiveDialog = new UserDataDialog();

        public static void Render(ResDict<UserData> userDataDict)
        {
            if (ImGui.Button($"   {IconManager.ADD_ICON}   "))
            {
                var userData = new UserData();
                userData.Name = " ";
                ShowDialog(userDataDict, userData);
            }

            var diabledTextColor = ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
            bool isDisabledEdit = Selected.Count == 0;
            if (isDisabledEdit)
                ImGui.PushStyleColor(ImGuiCol.Text, diabledTextColor);

            ImGui.SameLine();

            bool removed = ImGui.Button($"   {IconManager.DELETE_ICON}   ") && Selected.Count > 0;

            ImGui.SameLine();
            if (ImGui.Button($"   {IconManager.EDIT_ICON}   ") && Selected.Count > 0) {
                EditUserData(userDataDict, Selected[0]);
            }

            ImGui.SameLine();
            if (ImGui.Button($"   {IconManager.COPY_ICON}   ") && Selected.Count > 0)
            {
                Dictionary<string, object> usd = new Dictionary<string, object>();
                foreach (var param in Selected)
                    usd.Add($"{param.Type}|{param.Name}", param.GetData());
                ImGui.SetClipboardText(Newtonsoft.Json.JsonConvert.SerializeObject(usd));
            }
            ImGui.SameLine();
            if (ImGui.Button($"   {IconManager.PASTE_ICON}   ") && Selected.Count > 0)
            {
                var json = ImGui.GetClipboardText();
                var usd = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (usd == null)
                    return;

                var userData = BfresLibrary.TextConvert.UserDataConvert.Convert(usd);
                foreach (var userEntry in userData.Values)
                {
                    if (!userDataDict.ContainsKey(userEntry.Name))
                        userDataDict.Add(userEntry.Name, userEntry);
                    else
                        userDataDict[userEntry.Name] = userEntry;
                }
            }

            if (isDisabledEdit)
                ImGui.PopStyleColor();

            RenderHeader();

            ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg]);

            if (ImGui.BeginChild("USER_DATA_LIST"))
            {
                int index = 0;
                foreach (var userData in userDataDict.Values)
                {
                    bool isSelected = Selected.Contains(userData);

                    ImGui.Columns(2);
                    if (ImGui.Selectable(userData.Name, isSelected, ImGuiSelectableFlags.SpanAllColumns))
                    {
                        if(!ImGui.GetIO().KeyCtrl && !ImGui.GetIO().KeyShift)
                            Selected.Clear();

                        Selected.Add(userData);
                    }
                    if (isSelected && ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
                        EditUserData(userDataDict, userData);

                    ImGui.NextColumn();
                    ImGui.Text(GetDataString(userData, ","));
                    ImGui.NextColumn();

                    if (isSelected && ImGui.IsMouseDoubleClicked(0)) {
                        ImGui.OpenPopup("##user_data_dialog");
                    }
                    index++;

                    ImGui.Columns(1);
                }
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();

            if (removed)
            {
                foreach (var usd in Selected)
                    userDataDict.Remove(usd);
                Selected.Clear();
            }
        }

        static void EditUserData(ResDict<UserData> userDataDict, UserData selected)
        {
            //Apply data to new instance (so edits can be applied after)
            var userData = new UserData();
            userData.Name = selected.Name;
            if (selected.Type == UserDataType.Byte)        userData.SetValue(selected.GetValueByteArray());
            else if (selected.Type == UserDataType.Int32)  userData.SetValue(selected.GetValueInt32Array());
            else if (selected.Type == UserDataType.Single) userData.SetValue(Selected[0].GetValueSingleArray());
            else                                           userData.SetValue(selected.GetValueStringArray());

            ShowDialog(userDataDict, userData);
        }

        static void ShowDialog(ResDict<UserData> userDataDict, UserData userData)
        {
            string previousName = userData.Name;

            ActiveDialog.Load(userData);

            DialogHandler.Show("User Data", 300, 400, () =>
            {
                ActiveDialog.Render(userData);
            }, (ok) =>
            {
                if (!ok)
                    return;

                //Previous old entry
                if (previousName != userData.Name && userDataDict.ContainsKey(previousName))
                    userDataDict.RemoveKey(previousName);

                //Add new entry or overrite the existing one
                if (!userDataDict.ContainsKey(userData.Name))
                    userDataDict.Add(userData.Name, userData);
                else
                    userDataDict[userData.Name] = userData;

                Selected.Clear();
                Selected.Add(userData);
            });
        }

        static void RenderHeader()
        {
            ImGui.Columns(2);
            ImGuiHelper.BoldText(TranslationSource.GetText("NAME"));
            ImGui.NextColumn();
            ImGuiHelper.BoldText(TranslationSource.GetText("VALUE"));
            ImGui.Separator();
            ImGui.Columns(1);
        }

        static string GetDataString(UserData userData, string seperator = "\n")
        {
            if (userData.Type == UserDataType.Byte)
                return string.Join(seperator, userData.GetValueByteArray());
            else if (userData.Type == UserDataType.Int32)
                return string.Join(seperator, userData.GetValueInt32Array());
            else if (userData.Type == UserDataType.Single)
                return string.Join(seperator, userData.GetValueSingleArray());
            else
                return string.Join(seperator, userData.GetValueStringArray());

        }
    }
}
