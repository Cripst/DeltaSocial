public class Album
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ProfileId { get; set; }
    public Profile? Profile { get; set; }
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}
