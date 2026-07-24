using Vexis.Modeling;using Xunit;
namespace Vexis.Modeling.Tests;
public sealed class MeshOperationsTests
{
 [Fact] public void CubeHasExpectedTopology(){var m=MeshFactory.Cube();Assert.Equal(8,m.Vertices.Count);Assert.Equal(12,m.Edges.Count);Assert.Equal(6,m.Faces.Count);}
 [Fact] public void ExtrudeAddsSideFacesAndVertices(){var m=MeshFactory.Cube();m.SelectionMode=MeshSelectionMode.Face;m.SelectedFaces.Add(0);var v=m.Vertices.Count;var f=m.Faces.Count;MeshOperations.ExtrudeSelectedFaces(m,1);Assert.Equal(v+4,m.Vertices.Count);Assert.Equal(f+4,m.Faces.Count);}
}
