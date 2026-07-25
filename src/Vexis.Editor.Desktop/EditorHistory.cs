namespace Vexis.Editor.Desktop;

public interface IEditorOperation
{
    string Description { get; }
    void Undo();
    void Redo();
}

public sealed class EditorHistory
{
    private readonly Stack<IEditorOperation> _undo = new();
    private readonly Stack<IEditorOperation> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string? UndoDescription => _undo.TryPeek(out var item) ? item.Description : null;
    public string? RedoDescription => _redo.TryPeek(out var item) ? item.Description : null;

    public void Record(IEditorOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        _undo.Push(operation);
        _redo.Clear();
    }

    public bool Undo()
    {
        if (!_undo.TryPop(out var operation)) return false;
        operation.Undo();
        _redo.Push(operation);
        return true;
    }

    public bool Redo()
    {
        if (!_redo.TryPop(out var operation)) return false;
        operation.Redo();
        _undo.Push(operation);
        return true;
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}

public sealed class DelegateEditorOperation(string description, Action undo, Action redo) : IEditorOperation
{
    public string Description { get; } = description;
    public void Undo() => undo();
    public void Redo() => redo();
}
