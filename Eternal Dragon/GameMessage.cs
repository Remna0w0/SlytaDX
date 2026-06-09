namespace RemnaBotService.Eternal_Dragon
{
    public class GameMessage
    {
        public string Text { get; set; } // The plain text fallback
        public string Title { get; set; } // Optional: Embed Title
        public string Description { get; set; } // Optional: Embed Description
        public bool IsEmbed => !string.IsNullOrEmpty(Title);
    }
}
