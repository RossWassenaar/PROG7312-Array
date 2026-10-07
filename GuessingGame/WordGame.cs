namespace GuessingGame;

public enum GuessOutcome
{
    Correct,
    Wrong,
    Repeated,
    Invalid,
    RoundOver
}

public class WordGame
{
    public static readonly string[] Words =
    {
        "apple", "banana", "cherry", "grape", "lemon",
        "mango", "peach", "melon", "guava", "papaya",
        "kiwi", "plum", "orange", "litchi", "apricot"
    };

    public const int MaxAttempts = 5;
    public const int CorrectPoints = 10;
    public const int WrongPenalty = 1;

    private readonly List<string> _wrongGuesses = new();

    public WordGame(Random random)
    {
        Secret = Words[random.Next(Words.Length)];
        StartTime = DateTime.Now;
    }

    public string Secret { get; }
    public int Score { get; private set; }
    public int Attempts { get; private set; }
    public bool IsWon { get; private set; }
    public bool IsOver => IsWon || Attempts >= MaxAttempts;
    public int GuessesLeft => MaxAttempts - Attempts;
    public IReadOnlyList<string> WrongGuesses => _wrongGuesses;

    public DateTime StartTime { get; }
    public DateTime? EndTime { get; private set; }
    public TimeSpan Elapsed => (EndTime ?? DateTime.Now) - StartTime;

    public GuessOutcome Guess(string input)
    {
        if (IsOver)
            return GuessOutcome.RoundOver;

        string guess = input.Trim().ToLowerInvariant();
        if (guess.Length == 0 || !guess.All(char.IsLetter))
            return GuessOutcome.Invalid;
        if (_wrongGuesses.Contains(guess))
            return GuessOutcome.Repeated;

        Attempts++;

        if (guess == Secret)
        {
            Score += CorrectPoints;
            IsWon = true;
            EndTime = DateTime.Now;
            return GuessOutcome.Correct;
        }

        Score -= WrongPenalty;
        _wrongGuesses.Add(guess);
        if (Attempts >= MaxAttempts)
            EndTime = DateTime.Now;
        return GuessOutcome.Wrong;
    }

    public IReadOnlyList<string> Hints
    {
        get
        {
            int misses = _wrongGuesses.Count;
            var hints = new List<string>();

            if (misses >= 1)
                hints.Add($"Hint: it starts with \"{char.ToUpperInvariant(Secret[0])}\"");
            if (misses >= 2)
                hints.Add($"It has {Secret.Length} letters");
            if (misses >= 3)
                hints.Add($"It ends with \"{char.ToUpperInvariant(Secret[^1])}\"");
            if (misses >= 4)
                hints.Add("Last try! Every second letter is showing");

            return hints;
        }
    }

    public string MaskedWord
    {
        get
        {
            int misses = _wrongGuesses.Count;

            if (IsOver)
                return Spaced(Secret);
            if (misses == 0)
                return "?  ?  ?";
            if (misses == 1)
                return $"{char.ToUpperInvariant(Secret[0])} . . .";

            var letters = new char[Secret.Length];
            for (int i = 0; i < Secret.Length; i++)
            {
                bool show = i == 0
                    || (misses >= 3 && i == Secret.Length - 1)
                    || (misses >= 4 && i % 2 == 0);
                letters[i] = show ? Secret[i] : '_';
            }
            return Spaced(new string(letters));
        }
    }

    private static string Spaced(string word) => string.Join(" ", word.ToUpperInvariant().ToCharArray());
}
