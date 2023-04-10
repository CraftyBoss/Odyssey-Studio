using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
