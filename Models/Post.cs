public class Post
{
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;
    public int ProfileId { get; set; }
    public Profile Profile { get; set; }
}
