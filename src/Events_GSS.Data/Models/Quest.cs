using System;
using System.Collections.Generic;
using System.Text;

namespace Events_GSS.Data.Models;

public class Quest
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; }= "";
    public int Difficulty { get; set; }= 3;
    public Quest? PrerequisiteQuest { get; set; } = null;
    
    
}
