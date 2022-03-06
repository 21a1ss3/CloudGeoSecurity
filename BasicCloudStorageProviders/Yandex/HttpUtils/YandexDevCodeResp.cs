using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Encryptor.Lib.Yandex
{
    internal class YandexDevCodeResp : YandexErrorResponse
    {
        [JsonPropertyName("device_code")]
        public string DeviceCode { get; set; }

        [JsonPropertyName("user_code")]
        public string UserCode { get; set; }

        [JsonPropertyName("verification_url")]
        public Uri VerificationUri { get; set; }

        [JsonPropertyName("interval")]
        public int Interval { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
