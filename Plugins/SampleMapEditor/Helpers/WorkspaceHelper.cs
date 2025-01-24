using GLFrameworkEngine;
using MapStudio.UI;

namespace RedStarLibrary.Helpers
{
    internal static class WorkspaceHelper
    {
        public static void RemoveRendererFromActiveScene(IDrawable drawable, bool undo = false)
        {
            var activeWorkspace = Workspace.ActiveWorkspace;

            if (activeWorkspace == null)
                return;

            var activeScene = Workspace.ActiveWorkspace.ViewportWindow.Pipeline._context.Scene;

            if (activeScene == null)
                return;

            if (activeScene.Objects.Contains(drawable))
                activeScene.RemoveRenderObject(drawable, undo);
        }
    }
}
