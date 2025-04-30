namespace CustardRM.Services.AI;

public static class SentimentAnalysisService
{
	private static readonly HashSet<string> StopWords = new()
	{
		"a", "an", "the", "to", "and", "or", "of", "is", "in", "on", "it", "this", "that", "was", "i", "you", "he", "she", "they", "we", "it's"
	};

	private static List<string> Tokenize(string review)
	{
		return review
			.ToLowerInvariant()
			.Split(new[] { ' ', ',', '.', '!', '?', ':', ';', '-', '\r', '\n', '\t', '(', ')', '"' }, StringSplitOptions.RemoveEmptyEntries)
			.Where(word => !StopWords.Contains(word))
			.ToList();
	}

	private static Dictionary<string, int> GetBigramFrequencies(string review)
	{
		var bigrams = new Dictionary<string, int>();

		var tokens = Tokenize(review);
		for (int i = 0; i < tokens.Count - 1; i++)
		{
			var pair = $"{tokens[i]} {tokens[i + 1]}";
			if (bigrams.ContainsKey(pair))
				bigrams[pair]++;
			else
				bigrams[pair] = 1;
		}

		return bigrams
			.OrderByDescending(pair => pair.Value)
			.ToDictionary(pair => pair.Key, pair => pair.Value);
	}

	private static string PredictSentiment(string data)
	{
		var inputData = new ReviewSentimentAnalysisModel.ModelInput()
		{
			Review = data,
		};

		var result = ReviewSentimentAnalysisModel.Predict(inputData);

		if (result.PredictedLabel == 0)
		{
			return "negative";
		}
		else
		{
			return "positive";
		}
	}

	public static ReviewAnalysisResult AnalyseReviewText(string review)
	{
		return new ReviewAnalysisResult()
		{
			Sentiment = PredictSentiment(review),
			BigramFrequencies = GetBigramFrequencies(review)
		};
	}

	public class ReviewAnalysisResult
	{
		public string Sentiment { get; set; }
		public Dictionary<string, int> BigramFrequencies { get; set; } = new();
	}
}
