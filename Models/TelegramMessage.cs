using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace TelegramChatViewer.Models
{
    public class TelegramMessage
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "message";

        [JsonProperty("date")]
        public string DateString { get; set; } = "";

        [JsonProperty("date_unixtime")]
        public long? DateUnixtime { get; set; }

        [JsonProperty("from")]
        public string From { get; set; } = "";

        [JsonProperty("from_id")]
        public string FromId { get; set; } = "";

        [JsonProperty("text")]
        [JsonConverter(typeof(FlexibleTextConverter))]
        public object Text { get; set; } = "";

        [JsonProperty("reply_to_message_id")]
        public int? ReplyToMessageId { get; set; }

        [JsonProperty("forwarded_from")]
        public string ForwardedFrom { get; set; } = "";

        [JsonProperty("photo")]
        public string Photo { get; set; } = "";

        [JsonProperty("file")]
        public string File { get; set; } = "";

        [JsonProperty("thumbnail")]
        public string Thumbnail { get; set; } = "";

        [JsonProperty("media_type")]
        public string MediaType { get; set; } = "";

        [JsonProperty("mime_type")]
        public string MimeType { get; set; } = "";

        [JsonProperty("duration_seconds")]
        public int? DurationSeconds { get; set; }

        [JsonProperty("width")]
        public int? Width { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("file_size")]
        public long? FileSize { get; set; }

        [JsonProperty("sticker_emoji")]
        public string StickerEmoji { get; set; } = "";

        // Service message properties
        [JsonProperty("actor")]
        public string Actor { get; set; } = "";

        [JsonProperty("action")]
        public string Action { get; set; } = "";

        [JsonProperty("title")]
        public string Title { get; set; } = "";

        [JsonProperty("members")]
        public List<string> Members { get; set; } = new List<string>();

        // Computed properties
        public DateTime ParsedDate
        {
            get
            {
                // Prefer the ISO date string format as it's more reliable and matches the source
                if (!string.IsNullOrEmpty(DateString) && DateTime.TryParse(DateString, out DateTime stringResult))
                {
                    return stringResult;
                }

                if (DateUnixtime.HasValue)
                {
                    // Use UTC time directly without conversion to preserve the original time
                    return DateTimeOffset.FromUnixTimeSeconds(DateUnixtime.Value).DateTime;
                }

                return DateTime.MinValue;
            }
        }

        public string DisplaySender => !string.IsNullOrEmpty(From) ? From : FromId;

        public bool IsOutgoing => DisplaySender.Equals("You", StringComparison.OrdinalIgnoreCase);

        public bool IsServiceMessage => Type == "service";

        public bool IsReply => ReplyToMessageId.HasValue;

        public bool IsForwarded => !string.IsNullOrEmpty(ForwardedFrom);

        public bool HasMedia => !string.IsNullOrEmpty(Photo) || !string.IsNullOrEmpty(File);

        public List<FormattedTextPart> FormattedText
        {
            get
            {
                var parts = new List<FormattedTextPart>();

                if (Text is string plainText)
                {
                    if (!string.IsNullOrEmpty(plainText))
                    {
                        parts.Add(new FormattedTextPart { Text = plainText, Type = "plain" });
                    }
                }
                else if (Text is JArray textArray)
                {
                    foreach (var item in textArray)
                    {
                        if (item is JValue value)
                        {
                            parts.Add(new FormattedTextPart { Text = value.ToString(), Type = "plain" });
                        }
                        else if (item is JObject obj)
                        {
                            var part = new FormattedTextPart
                            {
                                Text = obj["text"]?.ToString() ?? "",
                                Type = obj["type"]?.ToString() ?? "plain",
                                Href = obj["href"]?.ToString() ?? ""
                            };
                            parts.Add(part);
                        }
                    }
                }
                else if (Text is Newtonsoft.Json.Linq.JArray legacyArray)
                {
                    // Handle legacy format for backward compatibility
                    foreach (var item in legacyArray)
                    {
                        if (item is Newtonsoft.Json.Linq.JValue value)
                        {
                            parts.Add(new FormattedTextPart { Text = value.ToString(), Type = "plain" });
                        }
                        else if (item is Newtonsoft.Json.Linq.JObject obj)
                        {
                            var part = new FormattedTextPart
                            {
                                Text = obj["text"]?.ToString() ?? "",
                                Type = obj["type"]?.ToString() ?? "plain",
                                Href = obj["href"]?.ToString() ?? ""
                            };
                            parts.Add(part);
                        }
                    }
                }

                return parts;
            }
        }

        public string PlainText
        {
            get
            {
                if (Text is string plainText)
                {
                    return plainText;
                }
                else if (Text is JArray textArray)
                {
                    var result = "";
                    foreach (var item in textArray)
                    {
                        if (item is JValue value)
                        {
                            result += value.ToString();
                        }
                        else if (item is JObject obj)
                        {
                            result += obj["text"]?.ToString() ?? "";
                        }
                    }
                    return result;
                }
                else if (Text is Newtonsoft.Json.Linq.JArray legacyArray)
                {
                    // Handle legacy format for backward compatibility
                    var result = "";
                    foreach (var item in legacyArray)
                    {
                        if (item is Newtonsoft.Json.Linq.JValue value)
                        {
                            result += value.ToString();
                        }
                        else if (item is Newtonsoft.Json.Linq.JObject obj)
                        {
                            result += obj["text"]?.ToString() ?? "";
                        }
                    }
                    return result;
                }
                return "";
            }
        }
    }

    public class FormattedTextPart
    {
        public string Text { get; set; } = "";
        public string Type { get; set; } = "plain";
        public string Href { get; set; } = "";
    }

    public class TelegramChat
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "Unknown Chat";

        [JsonProperty("type")]
        public string Type { get; set; } = "";

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("messages")]
        public List<TelegramMessage> Messages { get; set; } = new List<TelegramMessage>();
    }

    public class MediaInfo
    {
        public string Type { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string ThumbnailPath { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public long FileSize { get; set; }
        public int Duration { get; set; }
        public string StickerEmoji { get; set; } = "";
        public string MimeType { get; set; } = "";
    }

    public class FlexibleTextConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(object);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            
            if (token.Type == JTokenType.String)
            {
                return token.ToString();
            }
            else if (token.Type == JTokenType.Array)
            {
                return token; // Return the JArray as-is
            }
            else
            {
                return token.ToString();
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is string str)
            {
                writer.WriteValue(str);
            }
            else if (value is JToken token)
            {
                token.WriteTo(writer);
            }
            else
            {
                writer.WriteValue(value?.ToString() ?? "");
            }
        }
    }
} 