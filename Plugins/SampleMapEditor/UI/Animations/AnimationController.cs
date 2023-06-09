﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.Animations;
using UIFramework;
using MapStudio.UI;
using ImGuiNET;
using System.Numerics;

namespace SampleMapEditor
{
    /// <summary>
    /// Represents a playable animation that can be displayed in the viewport. 
    /// This gets activated when a tree node has been selected (attached by tag).
    /// To play these, hit the play button in the timeline or graph editor.
    /// </summary>
    internal class AnimationController : STAnimation, UIFramework.IEditableAnimation
    {
        public AnimationController()
        {
            Init();
        }

        /// <summary>
        /// The tree node loaded in the animation timeline.
        /// </summary>
        public TreeNode Root { get; set; }

        public void Init()
        {
            Name = "AnimationTest";
            //Amount of frames per second to animate
            FrameCount = 50;
            //Loop or not
            Loop = true;

            //Make a basic animaton using a color animation
            ColorGroup group = new ColorGroup();
            group.Name = "DiffuseColor";
            this.AnimGroups.Add(group);

            FloatGroup floatGroup = new FloatGroup();
            floatGroup.Name = "FloatTest";
            this.AnimGroups.Add(floatGroup);

            //Insert keys at frames with given values
            void SetChannel(STAnimationTrack track, float start, float end)
            {
                //Interpolate linear to gradually increase/decrease
                track.InterpolationType = STInterpoaltionType.Linear;
                track.KeyFrames.Add(new STKeyFrame(0, start));
                track.KeyFrames.Add(new STKeyFrame(25, end));
                track.KeyFrames.Add(new STKeyFrame(50, start));
            };

            SetChannel(group.R, 0, 1);
            SetChannel(group.G, 1, 0);
            SetChannel(group.B, 0, 1);

            SetChannel(floatGroup.Value, 0, 1000);

            PrepareGUI();
        }

        //Called during animation playback
        public override void NextFrame()
        {
            //The currently displayed frame
            var frame = this.Frame;

            //Loop through the groups to animate
            foreach (var group in this.AnimGroups)
            {
                if (group is ColorGroup) {
                    var color = (ColorGroup)group; 
                    //Get each color channel
                    float R = color.R.GetFrameValue(frame);
                    float G = color.G.GetFrameValue(frame);
                    float B = color.B.GetFrameValue(frame);

                    //As a test, use a renderer to visualize the changes
                    CustomRender.AnimatedColor = new OpenTK.Vector4(R, G, B, 1);
                }
            }
        }

        private void PrepareGUI()
        {
            //Prepare animation UI. These have their own tree system in the animation graph UI.
            //We need an AnimationTree.AnimNode and AnimationTree.TrackNode for displaying data
            Root = new AnimationTree.AnimNode(this);
            Root.Header = Name;

            foreach (var group in this.AnimGroups)
            {
                if (group is ColorGroup)
                {
                    var colorGroup = (ColorGroup)group;
                    //The animation needs a color node for displaying colors
                    var colorNodeUI = new AnimationTree.ColorGroupNode(this, group, group);
                    Root.AddChild(colorNodeUI);

                    //We want to add individual tracks for editing certain values
                    colorNodeUI.AddChild(new AnimationTree.TrackNode(this, colorGroup.R));
                    colorNodeUI.AddChild(new AnimationTree.TrackNode(this, colorGroup.G));
                    colorNodeUI.AddChild(new AnimationTree.TrackNode(this, colorGroup.B));
                }
                if (group is FloatGroup)
                {
                    TreeNode groupNode = new TreeNode(group.Name);
                    Root.AddChild(groupNode);

                    var fGroup = (FloatGroup)group;
                    //We want to add the tracks for editing certain values
                    groupNode.AddChild(new CustomUI(this, fGroup.Value));
                }
            }
        }

        /// <summary>
        /// Represents a custom animation graph tree node drawer
        /// </summary>
        class CustomUI : AnimationTree.TrackNode
        {
            public CustomUI(STAnimation anim, STAnimationTrack track) : base(anim, track)
            {

            }

            public override void RenderNode()
            {
                //Here we can draw our own UI.

                //Note for using booleans, the track interpolation type must be step for no interpolation!
                bool drawBoolean = false;

                //Draw the text for the track
                ImGui.Text(this.Header);
                //Next column (values are column based)
                ImGui.NextColumn();

                var color = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
                //Display keyed values differently
                bool isKeyed = Track.KeyFrames.Any(x => x.Frame == Anim.Frame);
                //Keyed color
                if (isKeyed)
                    color = new Vector4(0.602f, 0.569f, 0.240f, 1.000f);

                //Set the text color
                ImGui.PushStyleColor(ImGuiCol.Text, color);

                //Display the current track value
                float value = Track.GetFrameValue(Anim.Frame);
                //Span the whole column
                ImGui.PushItemWidth(ImGui.GetColumnWidth() - 3);
                //The editable track value. We could do booleans, floats, etc
                bool edited = false;

                if (drawBoolean)
                {
                    bool isChecked = value == 1;
                    edited = ImGui.Checkbox($"##{Track.Name}_frame", ref isChecked);
                    if (edited)
                        value = isChecked ? 1 : 0;
                }
                else
                {
                    edited = ImGui.DragFloat($"##{Track.Name}_frame", ref value);
                }
                bool isActive = ImGui.IsItemDeactivated();

                ImGui.PopItemWidth();

                //Insert key value from current frame
                if (edited || (isActive && ImGui.IsKeyDown((int)ImGuiKey.Enter)))
                    InsertOrUpdateKeyValue(value);

                ImGui.PopStyleColor();

                //Go to the next column
                ImGui.NextColumn();
            }
        }

        //Animations are divided into groups, with tracks to play animation data
        class ColorGroup : STAnimGroup
        {
            //A list of tracks per channel
            public STAnimationTrack R = new STAnimationTrack("R");
            public STAnimationTrack G = new STAnimationTrack("G");
            public STAnimationTrack B = new STAnimationTrack("B");

            //Get all the tracks used by this group
            public override List<STAnimationTrack> GetTracks()
            {
                return new List<STAnimationTrack>() { R, G, B };
            }
        }

        class FloatGroup : STAnimGroup
        {
            public STAnimationTrack Value = new STAnimationTrack("Value");
        }
    }
}
