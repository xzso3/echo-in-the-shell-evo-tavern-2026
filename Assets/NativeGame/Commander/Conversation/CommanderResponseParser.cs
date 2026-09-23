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
            out CommanderParsedResponse response, out string reason, out bool removedReasoningPrefix)
        {
            response = default;
            reason = null;
            removedReasoningPrefix = false;
            if (string.IsNullOrWhiteSpace(content)) { reason = "empty_content"; return false; }
            if (content.Length > 16384) { reason = "content_exceeds_16384"; return false; }
            if (snapshot == null) { reason = "missing_snapshot"; return false; }
            // Some compatible providers put reasoning in content rather than a
            // separate field. Accept only one explicit, leading, closed envelope;
            // never scan for JSON inside prose or repair a rejected proposal.
            string candidate = content.Trim();
            if (candidate.StartsWith("<think>", StringComparison.Ordinal))
            {
                int end = candidate.IndexOf("</think>", 7, StringComparison.Ordinal);
                int nested = candidate.IndexOf("<think>", 7, StringComparison.Ordinal);
                if (end < 0 || (nested >= 0 && nested < end))
                { reason = "reasoning_prefix_unclosed_or_nested"; return false; }
                candidate = candidate.Substring(end + "</think>".Length).Trim();
                removedReasoningPrefix = true;
                if (!candidate.StartsWith("{", StringComparison.Ordinal))
                { reason = "reasoning_prefix_not_followed_by_json_object"; return false; }
            }
            JObject root;
            try
            {
                using (var reader = new JsonTextReader(new System.IO.StringReader(candidate))
                { DateParseHandling = DateParseHandling.None, MaxDepth = 8 })
                {
                    root = JToken.ReadFrom(reader, new JsonLoadSettings
                    { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error }) as JObject;
                    if (root == null || reader.Read()) { reason = "root_not_object_or_trailing_content"; return false; }
                }
            }
            catch (JsonException) { reason = "invalid_json_or_duplicate_property_or_depth_exceeded"; return false; }
            if (root.Count != 2 || root["reply"]?.Type != JTokenType.String ||
                root["proposal"] == null) { reason = "root_requires_exactly_reply_string_and_proposal"; return false; }
            string reply = ((string)root["reply"])?.Trim();
            if (string.IsNullOrWhiteSpace(reply) || reply.Length > 1200) { reason = "reply_empty_or_exceeds_1200"; return false; }
            var proposal = root["proposal"];
            if (proposal.Type == JTokenType.Null)
            {
                response = new CommanderParsedResponse(reply, null, null);
                return true;
            }
            if (!(proposal is JObject fields) || fields.Count != 2 ||
                fields["kind"]?.Type != JTokenType.String ||
                fields["option_id"]?.Type != JTokenType.String) { reason = "proposal_requires_exactly_kind_and_option_id_strings"; return false; }
            string kindText = (string)fields["kind"];
            NativeSupportKind kind;
            if (kindText == "medical") kind = NativeSupportKind.Medical;
            else if (kindText == "weakpoint") kind = NativeSupportKind.Weakpoint;
            else { reason = "unknown_support_kind"; return false; }
            string optionId = (string)fields["option_id"];
            if (string.IsNullOrWhiteSpace(optionId) || optionId.Length > 128) { reason = "option_id_empty_or_exceeds_128"; return false; }
            bool matched = false;
            foreach (var option in snapshot.AvailableSupportOptions)
                if (option.SnapshotId == snapshot.SnapshotId && option.Kind == kind &&
                    string.Equals(option.OptionId, optionId, StringComparison.Ordinal))
                { matched = true; break; }
            if (!matched) { reason = "option_id_kind_snapshot_not_in_allowed_options"; return false; }
            response = new CommanderParsedResponse(reply, kind, optionId);
            return true;
        }
    }
}
