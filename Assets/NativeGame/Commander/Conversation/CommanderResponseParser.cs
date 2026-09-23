using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Echo.NativeGame.Commander
{
    internal readonly struct CommanderParsedResponse
    {
        internal readonly string Reply;
        internal readonly NativeSupportKind? Kind;
        internal readonly string OptionId;

        internal CommanderParsedResponse(string reply, NativeSupportKind? kind, string optionId)
        {
            Reply = reply;
            Kind = kind;
            OptionId = optionId;
        }
    }

    internal static class CommanderResponseParser
    {
        internal static bool TryParse(string content, CommanderSnapshot snapshot,
            out CommanderParsedResponse response)
        {
            response = default;
            if (string.IsNullOrWhiteSpace(content) || content.Length > 16384 || snapshot == null) return false;
            JObject root;
            try
            {
                using (var reader = new JsonTextReader(new System.IO.StringReader(content))
                { DateParseHandling = DateParseHandling.None, MaxDepth = 8 })
                {
                    root = JToken.ReadFrom(reader, new JsonLoadSettings
                    { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error }) as JObject;
                    if (root == null || reader.Read()) return false;
                }
            }
            catch (JsonException) { return false; }
            if (root.Count != 2 || root["reply"]?.Type != JTokenType.String ||
                root["proposal"] == null) return false;
            string reply = ((string)root["reply"])?.Trim();
            if (string.IsNullOrWhiteSpace(reply) || reply.Length > 1200) return false;
            var proposal = root["proposal"];
            if (proposal.Type == JTokenType.Null)
            {
                response = new CommanderParsedResponse(reply, null, null);
                return true;
            }
            if (!(proposal is JObject fields) || fields.Count != 2 ||
                fields["kind"]?.Type != JTokenType.String ||
                fields["option_id"]?.Type != JTokenType.String) return false;
            string kindText = (string)fields["kind"];
            NativeSupportKind kind;
            if (kindText == "medical") kind = NativeSupportKind.Medical;
            else if (kindText == "weakpoint") kind = NativeSupportKind.Weakpoint;
            else return false;
            string optionId = (string)fields["option_id"];
            if (string.IsNullOrWhiteSpace(optionId) || optionId.Length > 128) return false;
            bool matched = false;
            foreach (var option in snapshot.AvailableSupportOptions)
                if (option.SnapshotId == snapshot.SnapshotId && option.Kind == kind &&
                    string.Equals(option.OptionId, optionId, StringComparison.Ordinal))
                { matched = true; break; }
            if (!matched) return false;
            response = new CommanderParsedResponse(reply, kind, optionId);
            return true;
        }
    }
}
