using System;
using UnityEngine;

namespace Echo.LevelToolkit.Foundation
{
    // Authored identity survives export, import, and repeated placement of the same content.
    [Serializable]
    public struct ContentIdentity : IEquatable<ContentIdentity>
    {
        [SerializeField] private string authorId;
        [SerializeField] private string workId;
        [SerializeField] private string localId;

        public string AuthorId => authorId;
        public string WorkId => workId;
        public string LocalId => localId;
        public bool IsComplete => !string.IsNullOrWhiteSpace(authorId)
            && !string.IsNullOrWhiteSpace(workId)
            && !string.IsNullOrWhiteSpace(localId);

        public ContentIdentity(string authorId, string workId, string localId)
        {
            this.authorId = authorId;
            this.workId = workId;
            this.localId = localId;
        }

        public bool Equals(ContentIdentity other)
        {
            return string.Equals(authorId, other.authorId, StringComparison.Ordinal)
                && string.Equals(workId, other.workId, StringComparison.Ordinal)
                && string.Equals(localId, other.localId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is ContentIdentity other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(authorId ?? string.Empty);
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(workId ?? string.Empty);
                return hash * 31 + StringComparer.Ordinal.GetHashCode(localId ?? string.Empty);
            }
        }

        public override string ToString() => $"{authorId}/{workId}/{localId}";
    }
}
