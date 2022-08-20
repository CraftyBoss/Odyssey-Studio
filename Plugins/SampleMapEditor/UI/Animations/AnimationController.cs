using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.Animations;
using UIFramework;
using MapStudio.UI;

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
                    groupNode.AddChild(new AnimationTree.TrackNode(this, fGroup.Value));
                }
            }
        }

        //Animations are divided into groups, with tracks to play animation data
        class ColorGroup : STAnimGroup
        {
            //A list of tracks per channel
            public STAnimationTrack R = new STAnimationTrack("R");
            public STAnimationTrack G = new STAnimationTrack("R");
            public STAnimationTrack B = new STAnimationTrack("R");

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
