using HakoniwaByml.Iter;
using HakoniwaByml.Writer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedStarLibrary.Interfaces
{
    public interface IBymlSerializable
    {
        public void DeserializeByml(BymlIter rootNode);
        public BymlContainer SerializeByml();
    }
}
