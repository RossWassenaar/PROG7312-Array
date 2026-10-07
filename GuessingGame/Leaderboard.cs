namespace GuessingGame;

public record ScoreEntry(string Name, int Score, TimeSpan Time, string Word);

public class Leaderboard
{
    public const int Size = 5;

    private readonly List<ScoreEntry> _entries = new();

    public IReadOnlyList<ScoreEntry> Entries => _entries;

    public int Add(ScoreEntry entry)
    {
        _entries.Add(entry);
        _entries.Sort((a, b) => a.Score != b.Score
            ? b.Score.CompareTo(a.Score)
            : a.Time.CompareTo(b.Time));

        if (_entries.Count > Size)
            _entries.RemoveRange(Size, _entries.Count - Size);

        return _entries.FindIndex(e => ReferenceEquals(e, entry)) + 1;
    }
}
