using System;
using System.Collections.Generic;
using System.Text;

namespace Events_GSS.Data.Models;

public class QuestMemory
{
    public Quest ForQuest { get; set; }
    public Memory Proof { get; set; }
    public QuestMemoryStatus ProofStatus { get; set; } = QuestMemoryStatus.Submitted;
  
}
