public class Message
{
    public int Id { get; set; }
    public string SenderId { get; set; }
    public string ReceiverId { get; set; }
    public bool IsGroupMessage { get; set; }
    public string Content { get; set; }
    public DateTime SentAt { get; set; } = DateTime.Now;
}
