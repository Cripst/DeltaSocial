public class Comment
{
    public int Id { get; set; }
    public string Text { get; set; }
    public bool Approved { get; set; } = false;
    public int PhotoId { get; set; }
    public Photo Photo { get; set; }
    public string UserId { get; set; }
}
