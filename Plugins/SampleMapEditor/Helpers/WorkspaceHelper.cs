using GLFrameworkEngine;
using MapStudio.UI;
using System.IO;

namespace RedStarLibrary.Helpers
{
    internal static class WorkspaceHelper
    {
        public static void RemoveRendererFromActiveScene(IDrawable drawable, bool undo = false)
        {
            var activeScene = GLContext.ActiveContext.Scene;

            if (activeScene == null)
                return;

            if (activeScene.Objects.Contains(drawable))
                activeScene.RemoveRenderObject(drawable, undo);
        }

        public static void AddRendererToLoader(IDrawable drawable, bool undo = false)
        {
            var loader = GetCurrentEditorLoader();

            if (loader == null)
                return;

            loader.AddRender(drawable, undo);
        }

        public static PlacementFileEditor GetCurrentEditorLoader()
        {
            var activeWorkspace = Workspace.ActiveWorkspace;
            if (activeWorkspace == null)
                return null;

            if(activeWorkspace.ActiveEditor is PlacementFileEditor loader)
                return loader;
            else
                return null;
        }

        public static StageScene GetCurrentStageScene()
        {
            var activeEditor = GetCurrentEditorLoader();
            if(activeEditor == null)
                return null;
            return activeEditor.CurrentMapScene;
        }

        public static string WorkingDirectory => Directory.GetParent(Workspace.ActiveWorkspace.Resources.ProjectFile.WorkingDirectory).FullName;
    }
}
