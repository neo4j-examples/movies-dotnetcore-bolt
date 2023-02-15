using System.Collections.Generic;

namespace MoviesDotNetCore.Model;

public record D3Graph(IEnumerable<D3Node> Nodes, IEnumerable<D3Link> Links);

public record D3Node(string Title, string Label);

public record D3Link(int Source, int Target);