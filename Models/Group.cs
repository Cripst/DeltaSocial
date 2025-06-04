public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Profile> Members { get; set; } = new List<Profile>();
}
