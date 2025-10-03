using System.Text.Json;
using WhatsAppWebhook.Models.ConnectionCloud;

namespace WhatsAppWebhook.Services
{
    public class WebhookParser
    {
        private static readonly Dictionary<string, Func<JsonElement, string>> ContentExtractors = new()
        {
            ["text"] = msg => msg.GetProperty("text").GetProperty("body").GetString() ?? string.Empty,
            ["audio"] = msg => msg.GetProperty("audio").GetProperty("id").GetString() ?? string.Empty,
            ["interactive"] = msg =>
            {
                if (msg.TryGetProperty("interactive", out var interactive) &&
                    interactive.TryGetProperty("nfm_reply", out var nfmReply) &&
                    nfmReply.TryGetProperty("response_json", out var responseJsonProp))
                {
                    var responseJson = responseJsonProp.GetString();
                    if (!string.IsNullOrEmpty(responseJson))
                    {
                        using var doc = JsonDocument.Parse(responseJson);
                        var root = doc.RootElement;

                        var rating = root.TryGetProperty("screen_0_Elige_una_opcin_0", out var ratingProp)
                            ? ratingProp.GetString()
                            : string.Empty;

                        var comments = root.TryGetProperty("screen_0_TextArea_1", out var commentProp)
                            ? commentProp.GetString()
                            : string.Empty;

                        return $"{rating}|{comments}";
                    }
                }
                return string.Empty;
            }
        };

        public static IEnumerable<MessageLog> Parse(string rawBody)
        {
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            if (!root.TryGetProperty("entry", out var entries)) yield break;

            foreach (var entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("changes", out var changes)) continue;

                foreach (var change in changes.EnumerateArray())
                {
                    if (!change.TryGetProperty("value", out var value)) continue;
                    if (!value.TryGetProperty("messages", out var messages)) continue;

                    var phoneNumberId = value.GetProperty("metadata").GetProperty("phone_number_id").GetString() ?? string.Empty;

                    string senderName = string.Empty;
                    if (value.TryGetProperty("contacts", out var contacts))
                    {
                        senderName = contacts[0].GetProperty("profile").GetProperty("name").GetString() ?? string.Empty;
                    }

                    foreach (var msg in messages.EnumerateArray())
                    {
                        var type = msg.GetProperty("type").GetString() ?? "unknown";
                        var from = msg.GetProperty("from").GetString() ?? "unknown";
                        var msgId = msg.GetProperty("id").GetString() ?? string.Empty;
                        var timestamp = ParseTimestamp(msg);

                        string content = ContentExtractors.TryGetValue(type, out var extractor)
                            ? extractor(msg)
                            : string.Empty;

                        yield return new MessageLog
                        {
                            PhoneNumberId = phoneNumberId,
                            MessageId = msgId,
                            Sender = from,
                            SenderName = senderName,
                            Type = type,
                            Content = content,
                            TimestampUtc = timestamp,
                            RawPayload = rawBody
                        };
                    }
                }
            }
        }

        private static DateTime ParseTimestamp(JsonElement msg)
        {
            if (msg.TryGetProperty("timestamp", out var tsProp) &&
                long.TryParse(tsProp.GetString(), out var ts))
            {
                return DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime;
            }
            return DateTime.UtcNow;
        }
    }
}
