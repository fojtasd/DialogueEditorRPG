namespace DialogueSystem.Node {
    /// <summary>
    /// What kind of node accessibility is provided
    /// </summary>
    public enum AccessibilityKind {
        IsVisitableOnlyOnce = 0,
        NotVisitableAfterVisiting = 1,
        VisitableOnlyAfterVisiting = 2,
        AttributeRequired = 3,
        SkillRequired = 4,
        IntelRequired = 5,
        QuestRequired = 6,
        ItemRequired = 7,
        MoneyRequired = 8
    }
}