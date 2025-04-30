using System.ComponentModel.DataAnnotations;

namespace CustardRM.Models.DTOs.AI
{
    public class ReviewSentimentAnalysisModel
    {
        [Required(ErrorMessage = "Please enter a review")]
        public string Review { get; set; } = string.Empty;
    }
}
