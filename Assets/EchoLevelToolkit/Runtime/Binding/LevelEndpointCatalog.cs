using System;
using System.Collections.Generic;
using Echo.LevelToolkit.Foundation;
using UnityEngine;

namespace Echo.LevelToolkit.Binding
{
    public enum LevelEndpointKind
    {
        RegionEntered = 1,
        InteractionConfirmed = 2,
        EncounterCompleted = 3,
        ExitReached = 4,
        ActivateEncounter = 5,
        SetDoorOpen = 6,
        SetInteractionEnabled = 7,
        UnlockExit = 8
    }

    [Flags]
    public enum LevelBindingModes
    {
        None = 0,
        Sandbox = 1,
        Integrated = 2,
        Both = Sandbox | Integrated
    }

    public enum LevelRunMode
    {
        None = 0,
        Sandbox = 1,
        Integrated = 2
    }

    [Serializable]
    public sealed class LevelEndpointDefinition
    {
        [SerializeField] private ContentIdentity identity;
        [SerializeField] private LevelEndpointKind kind;
        [SerializeField] private string objectPath;
        [TextArea] [SerializeField] private string description;
        [SerializeField] private bool requiredBinding;
        [SerializeField] private LevelBindingModes allowedModes = LevelBindingModes.Both;

        public ContentIdentity Identity => identity;
        public LevelEndpointKind Kind => kind;
        public string ObjectPath => objectPath ?? string.Empty;
        public string Description => description ?? string.Empty;
        public bool RequiredBinding => requiredBinding;
        public LevelBindingModes AllowedModes => allowedModes;
        public bool IsEvent => kind >= LevelEndpointKind.RegionEntered && kind <= LevelEndpointKind.ExitReached;
        public bool IsAction => kind >= LevelEndpointKind.ActivateEncounter && kind <= LevelEndpointKind.UnlockExit;

        public bool Allows(LevelRunMode mode)
        {
            return mode == LevelRunMode.Sandbox && (allowedModes & LevelBindingModes.Sandbox) != 0
                || mode == LevelRunMode.Integrated && (allowedModes & LevelBindingModes.Integrated) != 0;
        }
    }

    [CreateAssetMenu(menuName = "Echo/Level Toolkit/Endpoint Catalog")]
    public sealed class LevelEndpointCatalog : ScriptableObject
    {
        [SerializeField] private ContentIdentity levelIdentity;
        [SerializeField] private LevelEndpointDefinition[] endpoints = Array.Empty<LevelEndpointDefinition>();

        public ContentIdentity LevelIdentity => levelIdentity;
        public IReadOnlyList<LevelEndpointDefinition> Endpoints => endpoints ?? Array.Empty<LevelEndpointDefinition>();

        public bool TryGet(ContentIdentity identity, out LevelEndpointDefinition endpoint)
        {
            foreach (LevelEndpointDefinition item in Endpoints)
            {
                if (item != null && item.Identity.Equals(identity))
                {
                    endpoint = item;
                    return true;
                }
            }
            endpoint = null;
            return false;
        }

        public IReadOnlyList<string> ValidateCatalog()
        {
            var errors = new List<string>();
            if (!levelIdentity.IsComplete)
                errors.Add("Level content identity is incomplete.");
            var seen = new HashSet<ContentIdentity>();
            foreach (LevelEndpointDefinition endpoint in Endpoints)
            {
                if (endpoint == null)
                {
                    errors.Add("Null endpoint entry.");
                    continue;
                }
                ContentIdentity id = endpoint.Identity;
                if (!id.IsComplete)
                    errors.Add("Endpoint identity is incomplete.");
                else if (!seen.Add(id))
                    errors.Add($"Duplicate endpoint ID: {id}.");
                if (id.IsComplete && levelIdentity.IsComplete
                    && (!string.Equals(id.AuthorId, levelIdentity.AuthorId, StringComparison.Ordinal)
                        || !string.Equals(id.WorkId, levelIdentity.WorkId, StringComparison.Ordinal)))
                    errors.Add($"Endpoint {id} belongs to another work.");
                if (!endpoint.IsEvent && !endpoint.IsAction)
                    errors.Add($"Endpoint {id} has an unknown kind.");
                if (endpoint.AllowedModes == LevelBindingModes.None
                    || (endpoint.AllowedModes & ~LevelBindingModes.Both) != 0)
                    errors.Add($"Endpoint {id} has invalid allowed modes.");
                if (string.IsNullOrWhiteSpace(endpoint.ObjectPath))
                    errors.Add($"Endpoint {id} has no object path.");
            }
            return errors;
        }

        public string GenerateManifestJson()
        {
            IReadOnlyList<string> errors = ValidateCatalog();
            if (errors.Count != 0)
                throw new InvalidOperationException(string.Join("; ", errors));
            var entries = new EndpointManifestEntry[Endpoints.Count];
            for (int i = 0; i < entries.Length; i++)
            {
                LevelEndpointDefinition endpoint = Endpoints[i];
                entries[i] = new EndpointManifestEntry
                {
                    id = endpoint.Identity.ToString(),
                    kind = endpoint.Kind.ToString(),
                    objectPath = endpoint.ObjectPath,
                    description = endpoint.Description,
                    requiredBinding = endpoint.RequiredBinding,
                    allowedModes = endpoint.AllowedModes.ToString()
                };
            }
            return JsonUtility.ToJson(new EndpointManifest
            {
                levelId = levelIdentity.ToString(),
                endpoints = entries
            }, true);
        }

        [Serializable]
        private sealed class EndpointManifest
        {
            public string levelId;
            public EndpointManifestEntry[] endpoints;
        }

        [Serializable]
        private sealed class EndpointManifestEntry
        {
            public string id;
            public string kind;
            public string objectPath;
            public string description;
            public bool requiredBinding;
            public string allowedModes;
        }
    }
}
