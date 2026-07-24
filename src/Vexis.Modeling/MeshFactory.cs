using System.Numerics;

namespace Vexis.Modeling;

public static class MeshFactory
{
    public static MeshDocument Cube(float size = 2f)
    {
        var h=size/2f; var m=new MeshDocument { Name="Cube" };
        m.Vertices.AddRange([
            new(-h,-h,-h), new(h,-h,-h), new(h,h,-h), new(-h,h,-h),
            new(-h,-h,h), new(h,-h,h), new(h,h,h), new(-h,h,h)]);
        m.Faces.AddRange([new([0,1,2,3]),new([4,7,6,5]),new([0,4,5,1]),new([3,2,6,7]),new([1,5,6,2]),new([0,3,7,4])]);
        RebuildEdges(m); return m;
    }
    public static MeshDocument Plane(float size=4f)
    {
        var h=size/2; var m=new MeshDocument{Name="Plane"};
        m.Vertices.AddRange([new(-h,0,-h),new(h,0,-h),new(h,0,h),new(-h,0,h)]);
        m.Faces.Add(new([0,1,2,3])); RebuildEdges(m); return m;
    }
    public static MeshDocument Cylinder(int segments=16,float radius=1,float height=2)
    {
        segments=Math.Clamp(segments,3,128); var m=new MeshDocument{Name="Cylinder"}; var h=height/2;
        for(int i=0;i<segments;i++){var a=MathF.Tau*i/segments; m.Vertices.Add(new(MathF.Cos(a)*radius,-h,MathF.Sin(a)*radius));}
        for(int i=0;i<segments;i++){var a=MathF.Tau*i/segments; m.Vertices.Add(new(MathF.Cos(a)*radius,h,MathF.Sin(a)*radius));}
        m.Faces.Add(new(Enumerable.Range(0,segments).Reverse().ToArray())); m.Faces.Add(new(Enumerable.Range(segments,segments).ToArray()));
        for(int i=0;i<segments;i++){var n=(i+1)%segments;m.Faces.Add(new([i,n,n+segments,i+segments]));}
        RebuildEdges(m); return m;
    }
    public static void RebuildEdges(MeshDocument mesh)
    {
        mesh.Edges.Clear(); var seen=new HashSet<(int,int)>();
        foreach(var f in mesh.Faces) for(int i=0;i<f.Indices.Count;i++){int a=f.Indices[i],b=f.Indices[(i+1)%f.Indices.Count];var k=a<b?(a,b):(b,a);if(seen.Add(k))mesh.Edges.Add(new(k.Item1,k.Item2));}
    }
}
