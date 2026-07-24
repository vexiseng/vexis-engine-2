using Vexis.Modeling;
namespace Vexis.Runtime;
public sealed class RuntimeSession
{
    public bool IsPlaying { get; private set; }
    public bool IsPaused { get; private set; }
    public TimeSpan Elapsed { get; private set; }
    public List<MeshDocument> SceneMeshes { get; }=[];
    public event Action? StateChanged;
    public void Play(){IsPlaying=true;IsPaused=false;StateChanged?.Invoke();}
    public void Pause(){if(IsPlaying)IsPaused=!IsPaused;StateChanged?.Invoke();}
    public void Stop(){IsPlaying=false;IsPaused=false;Elapsed=TimeSpan.Zero;StateChanged?.Invoke();}
    public void Tick(TimeSpan delta){if(IsPlaying&&!IsPaused)Elapsed+=delta;}
}
