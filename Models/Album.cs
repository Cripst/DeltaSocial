public class Album
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int ProfileId { get; set; }
    public Profile Profile { get; set; }
    public ICollection<Photo> Photos { get; set; }
}
