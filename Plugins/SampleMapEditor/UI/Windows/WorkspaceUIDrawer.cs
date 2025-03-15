using System;
using UIFramework;

namespace RedStarLibrary.UI.Windows
{
    internal class WorkspaceUIDrawer : DockWindow
    {
        private EventHandler UIDrawer;

        public WorkspaceUIDrawer(DockSpaceWindow parent, string name, EventHandler uiDrawer) : base(parent, name)
        {
            UIDrawer = uiDrawer;
        }

        public override void Render()
        {
            UIDrawer?.Invoke(this, EventArgs.Empty);
        }
    }
}
