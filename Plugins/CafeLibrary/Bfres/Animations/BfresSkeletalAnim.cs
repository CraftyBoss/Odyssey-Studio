﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.Animations;
using Toolbox.Core.ViewModels;
using Toolbox.Core;
using BfresLibrary;
using OpenTK;
using GLFrameworkEngine;
using MapStudio.UI;
using UIFramework;

namespace CafeLibrary.Rendering
{
    public class BfresSkeletalAnim : STSkeletonAnimation, IEditableAnimation, IContextMenu
    {
        //Root for animation tree
        public TreeNode Root { get; set; }

        public enum TrackType
        {
            XSCA = 0x4,
            YSCA = 0x8,
            ZSCA = 0xC,
            XPOS = 0x10,
            YPOS = 0x14,
            ZPOS = 0x18,
            XROT = 0x20,
            YROT = 0x24,
            ZROT = 0x28,
            WROT = 0x2C,
        }

        private string ModelName = null;

        public STSkeleton SkeletonOverride = null;

        public ResFile ResFile { get; set; }
        public SkeletalAnim SkeletalAnim { get; set; }

        public NodeBase UINode { get; set; }

        public BfresSkeletalAnim() { }

        public BfresSkeletalAnim(ResFile resFile, SkeletalAnim anim, string name)
        {
            ResFile = resFile;
            SkeletalAnim = anim;
            Root = new AnimationTree.AnimNode(this);
            UINode = new NodeBase(anim.Name) { Tag = this };
            UINode.CanRename = true;
            UINode.Icon = IconManager.SKELEAL_ANIM_ICON.ToString();
            UINode.OnHeaderRenamed += delegate
            {
                OnRenamed(UINode.Header);
            };

            ModelName = name;
            Reload(anim);
        }

        public BfresSkeletalAnim(SkeletalAnim anim, string name)
        {
            ModelName = name;
            Reload(anim);
        }

        public void OnRenamed(string name)
        {
            string previousName = SkeletalAnim.Name;

            SkeletalAnim.Name = name;
            if (ResFile.SkeletalAnims.ContainsKey(previousName))
            {
                ResFile.SkeletalAnims.RemoveKey(previousName);
                ResFile.SkeletalAnims.Add(SkeletalAnim.Name, SkeletalAnim);
            }
        }

        public MenuItemModel[] GetContextMenuItems()
        {
            return new MenuItemModel[]
            {
                new MenuItemModel("Export", ExportAction),
                new MenuItemModel("Replace", ReplaceAction),
                new MenuItemModel(""),
                new MenuItemModel("Rename", () => UINode.ActivateRename = true),
                new MenuItemModel(""),
                new MenuItemModel("Delete", DeleteAction)
            };
        }

        private void ExportAction()
        {
            var dlg = new ImguiFileDialog();
            dlg.SaveDialog = true;
            dlg.FileName = $"{SkeletalAnim.Name}.json";
            dlg.AddFilter(".bfska", ".bfska");
            dlg.AddFilter(".json", ".json");

            if (dlg.ShowDialog())
            {
                OnSave();
                SkeletalAnim.Export(dlg.FilePath, ResFile);
            }
        }

        private void ReplaceAction()
        {
            var dlg = new ImguiFileDialog();
            dlg.SaveDialog = false;
            dlg.FileName = $"{SkeletalAnim.Name}.json";
            dlg.AddFilter(".bfska", ".bfska");
            dlg.AddFilter(".json", ".json");

            if (dlg.ShowDialog())
            {
                SkeletalAnim.Import(dlg.FilePath, ResFile);
                Reload(SkeletalAnim);
            }
        }

        private void DeleteAction()
        {
            int result = TinyFileDialog.MessageBoxInfoYesNo("Are you sure you want to remove these animations? Operation cannot be undone.");
            if (result != 1)
                return;

            UINode.Parent.Children.Remove(UINode);

            if (ResFile.SkeletalAnims.ContainsValue(this.SkeletalAnim))
                ResFile.SkeletalAnims.Remove(this.SkeletalAnim);
        }

        public void InsertBoneKey(STBone bone, InsertFlags flags)
        {
            this.IsEdited = true;

            var group = (BoneAnimGroup)this.AnimGroups.FirstOrDefault(x => x.Name == bone.Name);
            //Add new material group if doesn't exist
            if (group == null)
            {
                group = new BoneAnimGroup() { Name = bone.Name, };
                this.AnimGroups.Add(group);
                //Add UI node
                if (!Root.Children.Any(x => x.Header == group.Name))
                    Root.AddChild(SkeletalAnimUI.GetGroupNode(this, group));
            }
            var boneNode = Root.Children.FirstOrDefault(x => x.Header == bone.Name);

            if (flags.HasFlag(InsertFlags.Position))
            {
                InsertKey(boneNode, group.Translate.X, bone.Position.X);
                InsertKey(boneNode, group.Translate.Y, bone.Position.Y);
                InsertKey(boneNode, group.Translate.Z, bone.Position.Z);
            }
            if (flags.HasFlag(InsertFlags.Rotation))
            {
                InsertKey(boneNode, group.Rotate.X, bone.EulerRotation.X);
                InsertKey(boneNode, group.Rotate.Y, bone.EulerRotation.Y);
                InsertKey(boneNode, group.Rotate.Z, bone.EulerRotation.Z);
            }
            if (flags.HasFlag(InsertFlags.Scale))
            {
                InsertKey(boneNode, group.Scale.X, bone.Scale.X);
                InsertKey(boneNode, group.Scale.Y, bone.Scale.Y);
                InsertKey(boneNode, group.Scale.Z, bone.Scale.Z);
            }
        }

        [Flags]
        public enum InsertFlags
        {
            Position = 1,
            Rotation = 2,
            Scale = 4,
        }

        private void InsertKey(TreeNode boneNode, STAnimationTrack track, float value)
        {
            var trackNode = (AnimationTree.TrackNode)boneNode.Children.FirstOrDefault(x => ((AnimationTree.TrackNode)x).Track == track);
            trackNode.InsertOrUpdateKeyValue(value);
        }

        public void OnSave() {
            if (IsEdited)
                SkeletalAnimConverter.ConvertAnimation(this, SkeletalAnim);
        }

        public BfresSkeletalAnim Clone()
        {
            BfresSkeletalAnim anim = new BfresSkeletalAnim();
            anim.ModelName = this.ModelName;
            anim.Name = this.Name;
            anim.FrameCount = this.FrameCount;
            anim.Frame = this.Frame;
            anim.Loop = this.Loop;
            anim.AnimGroups = this.AnimGroups;
            return anim;
        }

        public STSkeleton[] GetActiveSkeletons()
        {
            List<STSkeleton> skeletons = new List<STSkeleton>();
            if (SkeletonOverride != null)
            {
                skeletons.Add(SkeletonOverride);
                return skeletons.ToArray();
            }

            if (!DataCache.ModelCache.ContainsKey(ModelName))
                return null;

            var models = ((BfresRender)DataCache.ModelCache[ModelName]).Models;
            if (models.Count == 0) return null;

            if (!((BfresRender)DataCache.ModelCache[ModelName]).InFrustum)
                return null;

            foreach (var model in models)
            {
                if (model.IsVisible)
                    skeletons.Add(model.ModelData.Skeleton);
            }
            return skeletons.ToArray();
        }

        /// <summary>
        /// Gets the active skeleton visbile in the scene that may be used for animation.
        /// </summary>
        /// <returns></returns>
        public override STSkeleton GetActiveSkeleton()
        {
            if (!DataCache.ModelCache.ContainsKey(ModelName))
                return null;

            var models = ((BfresRender)DataCache.ModelCache[ModelName]).Models;
            if (models.Count == 0) return null;

            if (!((BfresRender)DataCache.ModelCache[ModelName]).InFrustum)
                return null;

            if (((BfresModelRender)models[0]).IsVisible)
                return ((BfresModelRender)models[0]).ModelData.Skeleton;
            return null;
        }

        public void Reload(SkeletalAnim anim)
        {
            Name = anim.Name;
            FrameCount = anim.FrameCount;
            FrameRate = 60.0f;
            Loop = anim.Loop;

            AnimGroups.Clear();
            foreach (var boneAnim in anim.BoneAnims)
            {
                var group = new BoneAnimGroup();
                AnimGroups.Add(group);

                group.Name = boneAnim.Name;
                if (anim.FlagsRotate == SkeletalAnimFlagsRotate.Quaternion)
                    group.UseQuaternion = true;

                float scale = GLContext.PreviewScale;

                //Set the base data for the first set of keys if used
                if (boneAnim.FlagsBase.HasFlag(BoneAnimFlagsBase.Translate))
                {
                    group.Translate.X.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Translate.X));
                    group.Translate.Y.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Translate.Y));
                    group.Translate.Z.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Translate.Z));
                }
                if (boneAnim.FlagsBase.HasFlag(BoneAnimFlagsBase.Rotate))
                {
                    group.Rotate.X.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Rotate.X));
                    group.Rotate.Y.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Rotate.Y));
                    group.Rotate.Z.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Rotate.Z));
                    group.Rotate.W.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Rotate.Z));
                }
                if (boneAnim.FlagsBase.HasFlag(BoneAnimFlagsBase.Scale))
                {
                    group.Scale.X.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Scale.X));
                    group.Scale.Y.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Scale.Y));
                    group.Scale.Z.KeyFrames.Add(new STKeyFrame(0, boneAnim.BaseData.Scale.Z));
                }

                if (boneAnim.ApplySegmentScaleCompensate)
                    group.UseSegmentScaleCompensate = true;

                //Generate keyed data from the curves
                foreach (var curve in boneAnim.Curves)
                {
                    switch ((TrackType)curve.AnimDataOffset)
                    {
                        case TrackType.XPOS: BfresAnimations.GenerateKeys(group.Translate.X, curve); break;
                        case TrackType.YPOS: BfresAnimations.GenerateKeys(group.Translate.Y, curve); break;
                        case TrackType.ZPOS: BfresAnimations.GenerateKeys(group.Translate.Z, curve); break;
                        case TrackType.XROT: BfresAnimations.GenerateKeys(group.Rotate.X, curve); break;
                        case TrackType.YROT: BfresAnimations.GenerateKeys(group.Rotate.Y, curve); break;
                        case TrackType.ZROT: BfresAnimations.GenerateKeys(group.Rotate.Z, curve); break;
                        case TrackType.WROT: BfresAnimations.GenerateKeys(group.Rotate.W, curve); break;
                        case TrackType.XSCA: BfresAnimations.GenerateKeys(group.Scale.X, curve); break;
                        case TrackType.YSCA: BfresAnimations.GenerateKeys(group.Scale.Y, curve); break;
                        case TrackType.ZSCA: BfresAnimations.GenerateKeys(group.Scale.Z, curve); break;
                    }
                }
            }
            if (ResFile != null)
                 SkeletalAnimUI.ReloadTree(Root, this, ResFile);
        }

        public override void NextFrame()
        {
            base.NextFrame();

            var skeletons = GetActiveSkeletons();
            if (skeletons == null || skeletons.Length == 0) return;

            foreach (var skeleton in skeletons)
                skeleton.Updated = false;

            foreach (var skeleton in skeletons)
                NextFrame(skeleton);

            MapStudio.UI.AnimationStats.SkeletalAnims += 1;
        }

        public bool NextFrame(STSkeleton skeleton)
        {
            //Skeleton instance updated already (can update via attachment from bone)
            if (skeleton.Updated)
                return false;

            bool update = false;

            foreach (var group in AnimGroups)
            {
                if (group is BoneAnimGroup)
                {
                    var boneAnim = (BoneAnimGroup)group;
                    STBone bone = skeleton.SearchBone(boneAnim.Name);
                    if (bone == null)
                        continue;


                    update = true;

                    Vector3 position = bone.Position;
                    Vector3 scale = bone.Scale;

                    if (boneAnim.Translate.X.HasKeys)
                        position.X = boneAnim.Translate.X.GetFrameValue(Frame) * GLContext.PreviewScale;
                    if (boneAnim.Translate.Y.HasKeys)
                        position.Y = boneAnim.Translate.Y.GetFrameValue(Frame) * GLContext.PreviewScale;
                    if (boneAnim.Translate.Z.HasKeys)
                        position.Z = boneAnim.Translate.Z.GetFrameValue(Frame) * GLContext.PreviewScale;

                    if (boneAnim.Scale.X.HasKeys)
                        scale.X = boneAnim.Scale.X.GetFrameValue(Frame);
                    if (boneAnim.Scale.Y.HasKeys)
                        scale.Y = boneAnim.Scale.Y.GetFrameValue(Frame);
                    if (boneAnim.Scale.Z.HasKeys)
                        scale.Z = boneAnim.Scale.Z.GetFrameValue(Frame);

                    bone.AnimationController.Position = position;
                    bone.AnimationController.Scale = scale;
                    bone.AnimationController.UseSegmentScaleCompensate = boneAnim.UseSegmentScaleCompensate;

                    if (boneAnim.UseQuaternion)
                    {
                        Quaternion rotation = bone.Rotation;

                        if (boneAnim.Rotate.X.HasKeys)
                            rotation.X = boneAnim.Rotate.X.GetFrameValue(Frame);
                        if (boneAnim.Rotate.Y.HasKeys)
                            rotation.Y = boneAnim.Rotate.Y.GetFrameValue(Frame);
                        if (boneAnim.Rotate.Z.HasKeys)
                            rotation.Z = boneAnim.Rotate.Z.GetFrameValue(Frame);
                        if (boneAnim.Rotate.W.HasKeys)
                            rotation.W = boneAnim.Rotate.W.GetFrameValue(Frame);

                        bone.AnimationController.Rotation = rotation;
                    }
                    else
                    {
                        Vector3 rotationEuluer = bone.EulerRotation;
                        if (boneAnim.Rotate.X.HasKeys)
                            rotationEuluer.X = boneAnim.Rotate.X.GetFrameValue(Frame);
                        if (boneAnim.Rotate.X.HasKeys)
                            rotationEuluer.Y = boneAnim.Rotate.Y.GetFrameValue(Frame);
                        if (boneAnim.Rotate.Z.HasKeys)
                            rotationEuluer.Z = boneAnim.Rotate.Z.GetFrameValue(Frame);

                        bone.AnimationController.EulerRotation = rotationEuluer;
                    }
                }
            }

            if (update)
                skeleton.Update();

            return update;
        }

        public class BoneAnimGroup : STAnimGroup
        {
            public Vector3Group Translate { get; set; }
            public Vector4Group Rotate { get; set; }
            public Vector3Group Scale { get; set; }

            public bool UseSegmentScaleCompensate { get; set; }

            public bool UseQuaternion = false;

            public BoneAnimGroup()
            {
                Translate = new Vector3Group() { Name = "Translate" };
                Rotate = new Vector4Group() { Name = "Rotate" };
                Scale = new Vector3Group() { Name = "Scale" };
                SubAnimGroups.Add(Translate);
                SubAnimGroups.Add(Rotate);
                SubAnimGroups.Add(Scale);
            }
        }

        public class Vector3Group : STAnimGroup
        {
            public BfresAnimationTrack X { get; set; }
            public BfresAnimationTrack Y { get; set; }
            public BfresAnimationTrack Z { get; set; }

            public Vector3Group()
            {
                X = new BfresAnimationTrack() { Name = "X", ChannelIndex = 0 };
                Y = new BfresAnimationTrack() { Name = "Y", ChannelIndex = 1 };
                Z = new BfresAnimationTrack() { Name = "Z", ChannelIndex = 2 };
            }

            public override List<STAnimationTrack> GetTracks()
            {
                List<STAnimationTrack> tracks = new List<STAnimationTrack>();
                tracks.Add(X);
                tracks.Add(Y);
                tracks.Add(Z);
                return tracks;
            }
        }

        public class Vector4Group : STAnimGroup
        {
            public BfresAnimationTrack X { get; set; }
            public BfresAnimationTrack Y { get; set; }
            public BfresAnimationTrack Z { get; set; }
            public BfresAnimationTrack W { get; set; }

            public Vector4Group()
            {
                X = new BfresAnimationTrack() { Name = "X", ChannelIndex = 0 };
                Y = new BfresAnimationTrack() { Name = "Y", ChannelIndex = 1 };
                Z = new BfresAnimationTrack() { Name = "Z", ChannelIndex = 2 };
                W = new BfresAnimationTrack() { Name = "W", ChannelIndex = 3 };
            }

            public override List<STAnimationTrack> GetTracks()
            {
                List<STAnimationTrack> tracks = new List<STAnimationTrack>();
                tracks.Add(X);
                tracks.Add(Y);
                tracks.Add(Z);
                return tracks;
            }
        }
    }
}
