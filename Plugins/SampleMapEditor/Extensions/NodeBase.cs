using Toolbox.Core.ViewModels;

namespace RedStarLibrary.Extensions
{
    internal static class NodeBaseExtensions
    {
        public static NodeBase GetChild(this NodeBase node, string name)
        {
            foreach (var child in node.Children)
                if (child.Header == name)
                    return child;
            return null;
        }
    }
}
