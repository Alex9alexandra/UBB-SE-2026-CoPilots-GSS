using System;
using System.Collections.Generic;
using System.Text;

namespace Events_GSS.Data.Models;


public class ReactionCounterDefault
{
    public const int DefaultTotal = 0;
}
public class ReactionCounter
{
    public int Count { get; set; } = ReactionCounterDefault.DefaultTotal;
    public string Emoji { get; set; } = string.Empty;
}
