
public sealed class DungeonSaveCoordinator : IDisposable
{
    private readonly string _dungeonId;
    private readonly DungeonState _state;
    private readonly Func<string, DungeonSaveData?> _load;
    private readonly Action<DungeonSaveData> _save;
    private readonly Func<bool> _isTransitioning;
    private readonly Action<Exception> _reportAutoSaveError;

    private bool _initialized;

    public DungeonSaveCoordinator(
        string dungeonId,
        DungeonState state,
        Func<string, DungeonSaveData?> load,
        Action<DungeonSaveData> save,
        Func<bool> isTransitioning,
        Action<Exception> reportAutoSaveError
    )
    {
        _dungeonId = dungeonId;
        _state = state;
        _load = load;
        _save = save;
        _isTransitioning = isTransitioning;
        _reportAutoSaveError = reportAutoSaveError;
    }

    public void Restore()
    {
        if (_initialized)
        {
            throw new InvalidOperationException(
                "Dungeon save coordinator is already initialized."
            );
        }

        var save = _load(_dungeonId);

        if (save is not null)
        {
            _state.Restore(save);
        }

        // Subscribe only after restoration to avoid
        // writing a save while loading an existing one.
        _state.Changed += OnStateChanged;
        _initialized = true;
    }

    public bool Save()
    {
        if (!_initialized || _isTransitioning())
        {
            return false;
        }

        var save = new DungeonSaveData
        {
            DungeonId = _dungeonId
        };

        _state.CopyTo(save);

        _save(save);

        return true;
    }

    private void OnStateChanged()
    {
        try
        {
            Save();
        }
        catch (Exception exception)
        {
            _reportAutoSaveError(exception);
        }
    }

    public void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        _state.Changed -= OnStateChanged;
        _initialized = false;
    }
}
