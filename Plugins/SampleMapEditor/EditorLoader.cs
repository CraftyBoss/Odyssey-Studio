using System;
using System.IO;
using Toolbox.Core;
using MapStudio.UI;
using OpenTK;
using GLFrameworkEngine;
using Toolbox.Core.IO;

namespace SampleMapEditor
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
        /// Determines when to use the map editor from a given file.
        /// You can check from file extension or check the data inside the file stream.
        /// The file stream is always decompressed if the given file has a supported ICompressionFormat like Yaz0.
        /// </summary>
        public bool Identify(File_Info fileInfo, Stream stream)
        {
            //For the sake of a demo, always load the plugin
            return true;
            //Example for loading as extension check
            return fileInfo.Extension == ".szs";
            //Example for checking inside the file for magic
            //NOTE you must use "true" in arguments to keep stream open
            using (var reader = new FileReader(stream, true))
            {
                return reader.CheckSignature(4, "MODL");
            }
        }

        /// <summary>
        /// Loads the given file data from a stream.
        /// </summary>
        public void Load(Stream stream)
        {
            //For this example I will show loading 3D objects into the scene
            MapScene scene = new MapScene();
            scene.Setup(this);
        }

        /// <summary>
        /// Saves the given file data to a stream.
        /// </summary>
        public void Save(Stream stream)
        {

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
    }
}
