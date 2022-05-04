﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GLFrameworkEngine;
using OpenTK;
using Toolbox.Core.ViewModels;
using MapStudio.UI;

namespace SampleMapEditor
{
    internal class MapScene
    {
        public void Setup(EditorLoader loader)
        {
            //Prepare a collision caster for snapping objects onto
            SetupSceneCollision();
            //Add some objects to the scene
            SetupObjects(loader);
        }

        /// <summary>
        /// Adds objects to the scene.
        /// </summary>
        private void SetupObjects(EditorLoader loader)
        {
            //A folder to represent in the outliner UI
            NodeBase folder = new NodeBase("Objects");
            //Allow toggling visibility for the folder
            folder.HasCheckBox = true;
            //Add it to the root of our loader
            //It is important you use "AddChild" so the parent is applied
            loader.Root.AddChild(folder);
            //Icons can be obtained from the icon manager constants
            //These also are all from font awesome and can be used directly
            folder.Icon = IconManager.MODEL_ICON.ToString();

            //These are default transform cubes
            //You give it the folder you want to parent in the tree or make it null to not be present.
            TransformableObject obj = new TransformableObject(folder);
            //Name
            obj.UINode.Header = "Object1";
            obj.UINode.Icon = IconManager.MESH_ICON.ToString();
            //Give it a transform in the scene
            obj.Transform.Position = new Vector3(0, 10, 0);
            obj.Transform.Scale = new Vector3(1, 1, 1);
            obj.Transform.RotationEulerDegrees = new Vector3(0, 0, 0);
            //You need to force update it. This is not updated per frame to save on performance
            obj.Transform.UpdateMatrix(true);

            //Lastly add your object to the scene
            loader.AddRender(obj);

            //Custom renderer
            CustomRender renderer = new CustomRender(folder);
            renderer.UINode.Icon = IconManager.MESH_ICON.ToString();
            renderer.UINode.Header = "Sphere";
            renderer.Transform.Position = new Vector3(-100, 0, 0);
            renderer.Transform.Scale = new Vector3(2.5f);
            renderer.Transform.UpdateMatrix(true);
            loader.AddRender(renderer);
        }

        /// <summary>
        /// Creates a big plane which you can drop objects onto.
        /// </summary>
        private void SetupSceneCollision()
        {
            var context = GLContext.ActiveContext;

            float size = 2000;
            float height = 0;

            //Make a big flat plane for placing spaces on.
            context.CollisionCaster.Clear();
            context.CollisionCaster.AddTri(
                new Vector3(-size, height, size),
                new Vector3(0, height, -(size * 2)),
                new Vector3(size * 2, height, 0));
            context.CollisionCaster.AddTri(
                new Vector3(-size, height, -size),
                new Vector3(size * 2, height, 0),
                new Vector3(size * 2, height, size * 2));
            context.CollisionCaster.UpdateCache();
        }
    }
}
