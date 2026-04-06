using System;
using System.Collections.Generic;

namespace Events_GSS.Data.Models;

public class CreateEventDto
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public bool IsPublic { get; set; }
    public string Description { get; set; } = "No description yet";
    public int? MaximumPeople { get; set; }
    public string? EventBannerPath { get; set; }
    public Category? Category { get; set; }
    public User? Admin { get; set; }
    public List<Quest> SelectedQuests { get; set; } = new();
}
