using System;
using System.Collections.Generic;
using System.Text;

namespace Events_GSS.Data.Models;

public class QuestMemory
{
    public required Quest ForQuest { get; set; }
    public required Memory Proof { get; set; }
    public QuestMemoryStatus ProofStatus { get; set; } = QuestMemoryStatus.Submitted;
  
}
