using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
