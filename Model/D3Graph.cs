using System;
using System.Collections.Generic;

namespace MoviesDotNetCore.Model
{
    public class D3Graph
    {
        public IEnumerable<D3Node> Nodes { get; }
        public IEnumerable<D3Link> Links { get; }

        public D3Graph(IEnumerable<D3Node> nodes, IEnumerable<D3Link> links)
        {
            Nodes = nodes;
            Links = links;
        }
    }

    public class D3Node
    {
        public string Title { get; }
        public string Label { get; }

        public D3Node(string title, string label)
        {
            Title = title;
            Label = label;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((D3Node) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Title, Label);
        }

        protected bool Equals(D3Node other)
        {
            return Title == other.Title && Label == other.Label;
        }
    }

    public class D3Link
    {
        public int Source { get; }
        public int Target { get; }

        public D3Link(int source, int target)
        {
            Source = source;
            Target = target;
        }
    }
}