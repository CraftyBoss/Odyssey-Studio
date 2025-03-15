using HakoniwaByml.Iter;
using HakoniwaByml.Writer;

namespace RedStarLibrary.Interfaces
{
    public interface IBymlSerializable
    {
        public void DeserializeByml(BymlIter rootNode);
        public BymlContainer SerializeByml();
    }
}
