using System;
using System.Collections.Generic;
using System.Text;

namespace Events_GSS.Data.Models;

public class AnnouncementReaction

{
    public int Id { get; set; } = 0;
    public required string Emoji { get; set; }
    public int AnnouncementId { get; set; } 
    public required User Author {  get; set; }
}
