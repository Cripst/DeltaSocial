public class Group
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Profile> Members { get; set; }

    public Group()
    {
        Members = new List<Profile>();
    }
}
