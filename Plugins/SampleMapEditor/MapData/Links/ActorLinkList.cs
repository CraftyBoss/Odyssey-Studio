using HakoniwaByml.Iter;
using HakoniwaByml.Writer;
using RedStarLibrary.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;

namespace RedStarLibrary.MapData.Links
{
    public class LinkEntry
    {

    }

    public class ActorLinkList : IEnumerable<LinkEntry>, IBymlSerializable
    {
        private List<LinkEntry> _linkList;
        private string linkName;

        public ActorLinkList(string name) { linkName = name; _linkList = new(); }

        public void DeserializeByml(BymlIter rootNode)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<LinkEntry> GetEnumerator()
        {
            foreach (LinkEntry layer in _linkList)
                yield return layer;
        }

        public BymlContainer SerializeByml()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
