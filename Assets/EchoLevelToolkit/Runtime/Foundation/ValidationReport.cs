using System;
using System.Collections.Generic;
using UnityEngine;

namespace Echo.LevelToolkit.Foundation
{
    public enum IssueSeverity { Info, Warning, Error }

    // Source and cell/side are optional locations. Stable Code is for UI and batch consumers.
    [Serializable]
    public sealed class ValidationIssue
    {
        [SerializeField] private IssueSeverity severity;
        [SerializeField] private string code;
        [SerializeField] private string message;
        [SerializeField] private string suggestedFix;
        [SerializeField] private UnityEngine.Object source;
        [SerializeField] private bool hasCell;
        [SerializeField] private Vector2Int cell;
        [SerializeField] private bool hasSide;
        [SerializeField] private ChunkSide side;

        public IssueSeverity Severity => severity;
        public string Code => code;
        public string Message => message;
        public string SuggestedFix => suggestedFix;
        public UnityEngine.Object Source => source;
        public bool HasCell => hasCell;
        public Vector2Int Cell => cell;
        public bool HasSide => hasSide;
        public ChunkSide Side => side;

        public ValidationIssue(IssueSeverity severity, string code, string message,
            string suggestedFix = null, UnityEngine.Object source = null,
            Vector2Int? cell = null, ChunkSide? side = null)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Issue code is required.", nameof(code));
            this.severity = severity;
            this.code = code;
            this.message = message ?? string.Empty;
            this.suggestedFix = suggestedFix ?? string.Empty;
            this.source = source;
            hasCell = cell.HasValue;
            this.cell = cell.GetValueOrDefault();
            hasSide = side.HasValue;
            this.side = side.GetValueOrDefault();
        }
    }

    [Serializable]
    public sealed class ValidationReport
    {
        [SerializeField] private List<ValidationIssue> issues = new List<ValidationIssue>();
        public IReadOnlyList<ValidationIssue> Issues => issues;
        public bool HasErrors
        {
            get
            {
                foreach (var issue in issues)
                    if (issue.Severity == IssueSeverity.Error) return true;
                return false;
            }
        }

        public void Add(ValidationIssue issue)
        {
            if (issue == null) throw new ArgumentNullException(nameof(issue));
            issues.Add(issue);
        }
    }
}
