using System.Collections.Generic;

namespace CoachMaster.Domain
{
    public sealed class CoachKnowledge
    {
        public Dictionary<string, List<string>> NotesByPlayerId { get; } = new Dictionary<string, List<string>>();
    }
}
