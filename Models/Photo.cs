﻿using System.Xml.Linq;

public class Photo
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int AlbumId { get; set; }
    public Album? Album { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
