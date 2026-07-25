namespace Vexis.Rendering;

public interface IRenderSceneBuilder<in TSource>
{
    void Build(TSource source, RenderScene destination);
}
