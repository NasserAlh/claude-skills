using OpenOsk.Core.Prediction;
using Xunit;

namespace OpenOsk.Core.Tests;

public class WordPredictorTests
{
    private static WordPredictor Loaded()
    {
        var p = new WordPredictor();
        p.LoadLexicon("the\nthere\nthen\nthese\nthem\nthanks\nhello\n");
        return p;
    }

    [Fact]
    public void PredictsByRankWithinPrefix()
    {
        var p = Loaded();
        Assert.Equal(["there", "then", "these"], p.Predict("the", 3));
    }

    [Fact]
    public void ExactMatchIsNotOffered()
    {
        var p = Loaded();
        Assert.DoesNotContain("hello", p.Predict("hello"));
        Assert.Empty(p.Predict("hello"));
    }

    [Fact]
    public void EmptyPrefixGivesNothing()
    {
        Assert.Empty(Loaded().Predict(string.Empty));
    }

    [Fact]
    public void LearnedWordsRankFirst()
    {
        var p = Loaded();
        p.Learn("thermostat");
        Assert.Equal("thermostat", p.Predict("the")[0]);
    }

    [Fact]
    public void ShortOrNonAlphabeticWordsAreNotLearned()
    {
        var p = Loaded();
        p.Learn("ab");
        p.Learn("x1y2");
        Assert.Empty(p.LearnedWords);
    }

    [Fact]
    public void ExplicitFrequenciesWin()
    {
        var p = new WordPredictor();
        p.LoadLexicon("apple\t5\napricot\t500\n");
        Assert.Equal(["apricot", "apple"], p.Predict("ap"));
    }

    [Theory]
    [InlineData("th", "there", "there")]
    [InlineData("Th", "there", "There")]
    [InlineData("TH", "there", "THERE")]
    public void MatchesUserCase(string prefix, string word, string expected)
    {
        Assert.Equal(expected, WordPredictor.MatchCase(word, prefix));
    }

    [Fact]
    public void CompletionIsTheSuffix()
    {
        Assert.Equal("re", WordPredictor.Completion("The", "There"));
        Assert.Equal("word", WordPredictor.Completion("xyz", "word"));
    }

    [Fact]
    public void BuiltInEnglishLexiconLoads()
    {
        var text = BuiltInLexicons.Read("en");
        Assert.NotNull(text);
        var p = new WordPredictor();
        p.LoadLexicon(text!);
        Assert.True(p.WordCount > 500);
        Assert.Contains("en", BuiltInLexicons.Available());
        Assert.NotEmpty(p.Predict("ke"));
    }

    [Fact]
    public void LearnedWordsRoundTripThroughStore()
    {
        var path = Path.Combine(Path.GetTempPath(), $"openosk-test-{Guid.NewGuid():N}.txt");
        try
        {
            var store = new LearnedWordsStore(path);
            var p = Loaded();
            p.Learn("keyboard");
            p.Learn("keyboard");
            store.Save(p.LearnedWords);
            var q = new WordPredictor();
            q.LoadLearned(store.Load());
            Assert.Equal(2, q.LearnedWords["keyboard"]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

public class TypedWordTrackerTests
{
    [Fact]
    public void TracksLettersAndBreaksOnSpace()
    {
        var t = new TypedWordTracker();
        string? completed = null;
        t.WordCompleted += (_, w) => completed = w;
        t.OnText("hel");
        Assert.Equal("hel", t.CurrentPrefix);
        t.OnBackspace();
        t.OnText("llo");
        Assert.Equal("hello", t.CurrentPrefix);
        t.OnCharacter(' ');
        Assert.Equal("hello", completed);
        Assert.False(t.HasPrefix);
    }

    [Fact]
    public void ResetDoesNotReportAWord()
    {
        var t = new TypedWordTracker();
        var fired = false;
        t.WordCompleted += (_, _) => fired = true;
        t.OnText("abc");
        t.Reset();
        Assert.False(fired);
        Assert.Equal(string.Empty, t.CurrentPrefix);
    }
}
