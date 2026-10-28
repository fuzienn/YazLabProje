namespace DungeonEscape.Dialogue
{
    public enum DialogueOutcome
    {
        Continue,
        DoorOpens,
        GuardAttacks
    }

    /// <summary>
    /// Modelin istedigi eylemi oyun kurallarina gore onaylar. Model "OPEN_DOOR" dese bile korku esigi
    /// asilmadiysa kapi acilmaz; "ATTACK" yalnizca gardiyan hala cesurken gecerlidir.
    /// </summary>
    public static class DialogueRules
    {
        public static DialogueOutcome Resolve(GuardDecision decision, FearMeter fear, out string note)
        {
            note = null;
            switch (decision.Action)
            {
                case GuardAction.OpenDoor when fear.CanOpenDoor:
                    return DialogueOutcome.DoorOpens;
                case GuardAction.OpenDoor:
                    note = $"Model kapiyi acmak istedi ama korku {fear.Level} < {FearMeter.OpenThreshold}; reddedildi.";
                    return DialogueOutcome.Continue;
                case GuardAction.Attack when fear.BraveEnoughToAttack:
                    return DialogueOutcome.GuardAttacks;
                case GuardAction.Attack:
                    note = $"Model saldirmak istedi ama gardiyan cok korkuyor ({fear.Level}); reddedildi.";
                    return DialogueOutcome.Continue;
                default:
                    return DialogueOutcome.Continue;
            }
        }
    }
}
