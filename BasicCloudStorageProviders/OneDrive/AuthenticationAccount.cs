using Azure.Identity;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Encryptor.Lib.OneDrive
{
    public class AuthenticationAccount : IAccount
    {
        private AuthenticationRecord _profile;

        internal AuthenticationAccount(AuthenticationRecord profile)
        {
            _profile = profile;
        }

        string IAccount.Username => _profile.Username;

        string IAccount.Environment => _profile.Authority;

        AccountId IAccount.HomeAccountId
        {
            get
            {
                try
                {
                    return (AccountId)(typeof(AuthenticationRecord)
                                           .GetProperty("AccountId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                           .GetMethod
                                           .Invoke(_profile, null));
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
    }
}
