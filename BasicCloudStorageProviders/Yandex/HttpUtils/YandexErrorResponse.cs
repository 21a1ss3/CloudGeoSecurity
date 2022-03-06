using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Encryptor.Lib.Yandex
{
    internal class YandexErrorResponse
    {
        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; }
        [JsonPropertyName("error")]
        public string ErrorCode { get; set; }
    }
}
