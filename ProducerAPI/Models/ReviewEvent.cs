namespace ProducerAPI.Models
{
    // In a file like Models/ReviewEvent.cs
    public class ReviewEvent
    {
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
    }
}
