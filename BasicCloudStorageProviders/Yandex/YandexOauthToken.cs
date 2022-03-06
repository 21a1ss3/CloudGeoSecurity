using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib.Yandex
{
    internal class YandexOauthToken
    {
        public YandexOauthToken(string accessToken, string refreshToken, string tokenType, string scope, DateTime issued, DateTime expired)
        {
            AccessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            RefreshToken = refreshToken ?? throw new ArgumentNullException(nameof(refreshToken));
            TokenType = tokenType ?? throw new ArgumentNullException(nameof(tokenType));
            Scopes = scope?.Split(' ')?? new string[0];
            Issued = issued;
            Expired = expired;
        }

        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public string TokenType { get; private set; }

        public string[] Scopes { get; private set; }

        public DateTime Issued { get; private set; }
        public DateTime Expired { get; private set; }
    }
}
