using System;
using System.Collections.Generic;
using System.Text;

namespace Events_GSS.Data.Models;

public class AnnouncementReadReceipt
{
    public int AnnouncementId { get; set; }
    public User User { get; set; }
    public DateTime ReadAt { get; set; }
}
