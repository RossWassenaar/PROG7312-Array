using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GuessingGame;

public record LeaderboardRow(int Rank, string Name, string Score, string Time, bool IsNew);

public partial class MainWindow : Window
{
    private readonly Random _random = new();
    private readonly Leaderboard _leaderboard = new();
    private readonly DispatcherTimer _stopwatch = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private WordGame _game = null!;
    private int _page;

    public MainWindow()
    {
        InitializeComponent();

        DateText.Text = DateTime.Today.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        _stopwatch.Tick += (_, _) => UpdateTimer();

        RefreshLeaderboard(newest: null);
        StartRound();
    }

    private void StartRound()
    {
        _game = new WordGame(_random);
        _page++;
        PageText.Text = $"Page {_page}";

        GuessBox.Clear();
        GuessBox.IsEnabled = true;
        GuessButton.IsEnabled = true;
        PlayArea.Visibility = Visibility.Visible;
        EndArea.Visibility = Visibility.Collapsed;
        FeedbackText.Text = "Five tries. Go!";

        Redraw();
        _stopwatch.Start();
        GuessBox.Focus();
    }

    private void SubmitGuess()
    {
        string guess = GuessBox.Text.Trim().ToLowerInvariant();
        GuessOutcome outcome = _game.Guess(guess);

        FeedbackText.Text = outcome switch
        {
            GuessOutcome.Correct => $"Yes! It's {guess}!",
            GuessOutcome.Wrong when _game.IsOver => $"Not \"{guess}\" either... out of tries!",
            GuessOutcome.Wrong => $"Nope, not \"{guess}\".  (−{WordGame.WrongPenalty})",
            GuessOutcome.Repeated => $"You already crossed out \"{guess}\" (no penalty)",
            GuessOutcome.Invalid => "Letters only, please!",
            _ => FeedbackText.Text
        };

        GuessBox.Clear();
        Redraw();

        if (_game.IsOver)
        {
            EndRound();
            return;
        }

        if (outcome != GuessOutcome.Correct)
            Wobble(GuessBox);
        GuessBox.Focus();
    }

    private void EndRound()
    {
        _stopwatch.Stop();
        UpdateTimer();
        GuessBox.IsEnabled = false;
        GuessButton.IsEnabled = false;

        string name = string.IsNullOrWhiteSpace(NameBox.Text) ? "Anonymous" : NameBox.Text.Trim();
        var entry = new ScoreEntry(name, _game.Score, _game.Elapsed, _game.Secret);
        int rank = _leaderboard.Add(entry);
        RefreshLeaderboard(entry);

        string tries = _game.Attempts == 1 ? "1 try" : $"{_game.Attempts} tries";
        StampText.Text = _game.IsWon ? $"Excellent! ★ +{_game.Score}" : "Oops!";
        StampNote.Text = (_game.IsWon ? $"Got it in {tries}" : $"It was {_game.Secret.ToUpperInvariant()}")
                         + (rank > 0 ? $"  ·  #{rank} on the board" : "  ·  not in the top 5");

        PlayArea.Visibility = Visibility.Collapsed;
        EndArea.Visibility = Visibility.Visible;
        SlamStamp();
        NextPageButton.Focus();
    }

    private void Redraw()
    {
        WordText.Text = _game.MaskedWord;
        WordText.Foreground = Pen(_game.IsOver && !_game.IsWon ? "RedPen" : "Ink");
        ScoreText.Text = _game.Score.ToString(CultureInfo.InvariantCulture);
        HintList.ItemsSource = _game.Hints;
        WrongList.ItemsSource = _game.WrongGuesses.ToList();
        CrossedOutRow.Visibility = _game.WrongGuesses.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateTimer();
        DrawTries();
    }

    private void DrawTries()
    {
        TriesPanel.Children.Clear();
        for (int i = 0; i < WordGame.MaxAttempts; i++)
        {
            string mark = i < _game.WrongGuesses.Count ? "✗"
                : _game.IsWon && i == _game.Attempts - 1 ? "✓"
                : "○";

            TriesPanel.Children.Add(new TextBlock
            {
                Text = mark,
                FontSize = 34,
                Margin = new Thickness(0, 0, 12, 0),
                Foreground = Pen(mark == "○" ? "Ink" : "RedPen")
            });
        }
    }

    private void RefreshLeaderboard(ScoreEntry? newest)
    {
        LeaderboardList.ItemsSource = _leaderboard.Entries
            .Select((e, i) => new LeaderboardRow(i + 1, e.Name, e.Score.ToString(CultureInfo.InvariantCulture),
                FormatTime(e.Time), ReferenceEquals(e, newest)))
            .ToList();
        LeaderboardEmpty.Visibility = _leaderboard.Entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateTimer() => TimerText.Text = $"⏱ {FormatTime(_game.Elapsed)}";

    private static string FormatTime(TimeSpan time) => $"{(int)time.TotalMinutes}:{time.Seconds:00}";

    private Brush Pen(string key) => (Brush)FindResource(key);

    private void SlamStamp()
    {
        var slam = new DoubleAnimation(2.4, 1, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 }
        };
        StampScale.BeginAnimation(ScaleTransform.ScaleXProperty, slam);
        StampScale.BeginAnimation(ScaleTransform.ScaleYProperty, slam);
        Stamp.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
    }

    private static void Wobble(UIElement element)
    {
        var shift = new TranslateTransform();
        element.RenderTransform = shift;

        double[] offsets = { -9, 8, -6, 4, -2, 0 };
        var shake = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(320) };
        for (int i = 0; i < offsets.Length; i++)
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(offsets[i], KeyTime.FromPercent((i + 1.0) / offsets.Length)));

        shift.BeginAnimation(TranslateTransform.XProperty, shake);
    }

    private void GuessButton_Click(object sender, RoutedEventArgs e) => SubmitGuess();

    private void GuessBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            SubmitGuess();
    }

    private void NextPageButton_Click(object sender, RoutedEventArgs e) => StartRound();

    private void NameBox_TextChanged(object sender, TextChangedEventArgs e) =>
        NameHint.Visibility = NameBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
}
