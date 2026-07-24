using System.Numerics;

namespace Vexis.Modeling;

public static class MeshOperations
{
    public static void Translate(MeshDocument mesh, Vector3 delta)
    { foreach(var i in TargetVertices(mesh)) mesh.Vertices[i]+=delta; }
    public static void Scale(MeshDocument mesh, Vector3 scale, Vector3 pivot)
    { foreach(var i in TargetVertices(mesh)){var d=mesh.Vertices[i]-pivot;mesh.Vertices[i]=pivot+new Vector3(d.X*scale.X,d.Y*scale.Y,d.Z*scale.Z);} }
    public static void RotateY(MeshDocument mesh,float radians,Vector3 pivot)
    { var q=Quaternion.CreateFromAxisAngle(Vector3.UnitY,radians);foreach(var i in TargetVertices(mesh))mesh.Vertices[i]=pivot+Vector3.Transform(mesh.Vertices[i]-pivot,q); }
    public static void DeleteSelected(MeshDocument mesh)
    {
        if(mesh.SelectionMode==MeshSelectionMode.Face && mesh.SelectedFaces.Count>0){for(int i=mesh.Faces.Count-1;i>=0;i--)if(mesh.SelectedFaces.Contains(i))mesh.Faces.RemoveAt(i);mesh.SelectedFaces.Clear();MeshFactory.RebuildEdges(mesh);}
    }
    public static void ExtrudeSelectedFaces(MeshDocument mesh,float distance)
    {
        var selected=mesh.SelectedFaces.OrderBy(i=>i).ToArray(); if(selected.Length==0)return;
        var newFaces=new List<MeshFace>(); var replacement=new List<(int index,MeshFace face)>();
        foreach(var fi in selected){var f=mesh.Faces[fi];var normal=FaceNormal(mesh,f);var map=new Dictionary<int,int>();
            foreach(var old in f.Indices){map[old]=mesh.Vertices.Count;mesh.Vertices.Add(mesh.Vertices[old]+normal*distance);} var top=new MeshFace(f.Indices.Select(i=>map[i]).ToArray()); replacement.Add((fi,top));
            for(int i=0;i<f.Indices.Count;i++){int a=f.Indices[i],b=f.Indices[(i+1)%f.Indices.Count];newFaces.Add(new([a,b,map[b],map[a]]));}}
        foreach(var (i,f) in replacement)mesh.Faces[i]=f;mesh.Faces.AddRange(newFaces);mesh.SelectedFaces.Clear();mesh.SelectedFaces.UnionWith(replacement.Select(x=>x.index));MeshFactory.RebuildEdges(mesh);
    }
    public static Vector3 SelectionCenter(MeshDocument mesh){var ids=TargetVertices(mesh).ToArray();if(ids.Length==0)return Vector3.Zero;var v=Vector3.Zero;foreach(var i in ids)v+=mesh.Vertices[i];return v/ids.Length;}
    public static Vector3 FaceNormal(MeshDocument mesh,MeshFace f){if(f.Indices.Count<3)return Vector3.UnitY;var a=mesh.Vertices[f.Indices[0]];var b=mesh.Vertices[f.Indices[1]];var c=mesh.Vertices[f.Indices[2]];var n=Vector3.Cross(b-a,c-a);return n.LengthSquared()<1e-8f?Vector3.UnitY:Vector3.Normalize(n);}
    private static IEnumerable<int> TargetVertices(MeshDocument mesh)
    {
        if(mesh.SelectionMode==MeshSelectionMode.Vertex && mesh.SelectedVertices.Count>0)return mesh.SelectedVertices;
        if(mesh.SelectionMode==MeshSelectionMode.Edge && mesh.SelectedEdges.Count>0)return mesh.SelectedEdges.SelectMany(i=>new[]{mesh.Edges[i].A,mesh.Edges[i].B}).Distinct();
        if(mesh.SelectionMode==MeshSelectionMode.Face && mesh.SelectedFaces.Count>0)return mesh.SelectedFaces.SelectMany(i=>mesh.Faces[i].Indices).Distinct();
        return Enumerable.Range(0,mesh.Vertices.Count);
    }
}
