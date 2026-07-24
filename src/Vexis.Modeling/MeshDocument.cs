using System.Numerics;

namespace Vexis.Modeling;

public enum MeshSelectionMode { Object, Vertex, Edge, Face }
public readonly record struct MeshEdge(int A, int B);
public sealed record MeshFace(IReadOnlyList<int> Indices);

public sealed class MeshDocument
{
    public List<Vector3> Vertices { get; } = [];
    public List<MeshEdge> Edges { get; } = [];
    public List<MeshFace> Faces { get; } = [];
    public HashSet<int> SelectedVertices { get; } = [];
    public HashSet<int> SelectedEdges { get; } = [];
    public HashSet<int> SelectedFaces { get; } = [];
    public MeshSelectionMode SelectionMode { get; set; } = MeshSelectionMode.Object;
    public string Name { get; set; } = "Untitled Mesh";

    public void ClearSelection() { SelectedVertices.Clear(); SelectedEdges.Clear(); SelectedFaces.Clear(); }
    public MeshDocument Clone()
    {
        var c = new MeshDocument { Name = Name, SelectionMode = SelectionMode };
        c.Vertices.AddRange(Vertices); c.Edges.AddRange(Edges); c.Faces.AddRange(Faces);
        c.SelectedVertices.UnionWith(SelectedVertices); c.SelectedEdges.UnionWith(SelectedEdges); c.SelectedFaces.UnionWith(SelectedFaces);
        return c;
    }
}
