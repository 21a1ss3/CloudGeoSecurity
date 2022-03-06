using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Microsoft.Identity.Client;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Encryptor.Lib.OneDrive
{
    //slightly modernised fork from MS Azure Identity lib src with its dependent types
    public class InteractiveBrowserCredentialExtended : InteractiveBrowserCredential
    {
        private readonly string _tenantId;
        protected string ClientId { get; }
        protected string LoginHint { get; }
        protected IPublicClientApplication Client { get; }
        protected bool DisableAutomaticAuthentication { get; }
        protected AuthenticationRecord Record { get; private set; }
        protected SystemWebViewOptions SystemWebViewOptions { get; private set; }

        private const string AuthenticationRequiredMessage = "Interactive authentication is needed to acquire token. Call Authenticate to interactively authenticate.";
        private const string NoDefaultScopeMessage = "Authenticating in this environment requires specifying a TokenRequestContext.";


        public InteractiveBrowserCredentialExtended()
            : this(null)
        { }


        public InteractiveBrowserCredentialExtended(InteractiveBrowserCredentialOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(options.ClientId))
                throw new ArgumentException("ClientId must be defined");


            ClientId = options.ClientId;
            _tenantId = options?.TenantId ?? "common";
            LoginHint = (options as InteractiveBrowserCredentialOptions)?.LoginHint;
            SystemWebViewOptions = (options as InteractiveBrowserCredentialOptionsExtended)?.WebViewOptions;

            var redirectUrl = (options as InteractiveBrowserCredentialOptions)?.RedirectUri?.AbsoluteUri ?? "http://localhost";
            DisableAutomaticAuthentication = options?.DisableAutomaticAuthentication ?? false;
            Record = options?.AuthenticationRecord;

            Client = PublicClientApplicationBuilder.Create(ClientId)
                                                   .WithRedirectUri(redirectUrl)
                                                   .Build();
        }



        public override AuthenticationRecord Authenticate(CancellationToken cancellationToken = default)
        {
            return Authenticate(new TokenRequestContext(new[] { "https://management.core.windows.net//.default" }), cancellationToken);
        }


        public override AuthenticationRecord Authenticate(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
        {
            return _authenticateCoreAsync(requestContext, cancellationToken, false).GetAwaiter().GetResult();
        }

        public override async Task<AuthenticationRecord> AuthenticateAsync(CancellationToken cancellationToken = default)
        {
            // get the default scope for the authority, throw if no default scope exists
            string defaultScope = "https://management.core.windows.net//.default" ?? throw new CredentialUnavailableException(NoDefaultScopeMessage);

            return await AuthenticateAsync(new TokenRequestContext(new string[] { defaultScope }), cancellationToken).ConfigureAwait(false);
        }

        public override async Task<AuthenticationRecord> AuthenticateAsync(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
        {
            return await _authenticateCoreAsync(requestContext, cancellationToken, true).ConfigureAwait(false);
        }

        private async ValueTask<AuthenticationRecord> _authenticateCoreAsync(TokenRequestContext requestContext, CancellationToken cancellationToken, bool async)
        {
            await GetTokenViaBrowserLoginAsync(requestContext, cancellationToken, async).ConfigureAwait(false);
            return Record;
        }

        private async Task<AccessToken> GetTokenViaBrowserLoginAsync(TokenRequestContext context, CancellationToken cancellationToken, bool async)
        {
            Prompt prompt = LoginHint switch
            {
                null => Prompt.SelectAccount,
                _ => Prompt.NoPrompt
            };

            Func<Task<AuthenticationResult>> coreCallback = async () =>
            {
                var tokenBuilder = Client.AcquireTokenInteractive(context.Scopes)
                                         .WithPrompt(prompt)
                                         .WithClaims(context.Claims)
                                         .WithPrompt(prompt)
                                         .WithClaims(context.Claims);

                if (LoginHint != null)
                    tokenBuilder.WithLoginHint(LoginHint);

                if (_tenantId != null)
                    tokenBuilder.WithTenantId(_tenantId);

                if (SystemWebViewOptions != null)
                    tokenBuilder.WithSystemWebViewOptions(SystemWebViewOptions);

                return await tokenBuilder.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            };

            AuthenticationResult result;
            if (async)
                result = await coreCallback();
            else
                result = await Task.Run(coreCallback);

            Record = (AuthenticationRecord)(typeof(AuthenticationRecord)
                                                .GetConstructor(
                                                    BindingFlags.NonPublic | BindingFlags.Instance, 
                                                    Type.DefaultBinder, 
                                                    new Type[] { typeof(AuthenticationResult), typeof(string) }, 
                                                    null)
                                                .Invoke(new object[] { result, ClientId }));
            return new AccessToken(result.AccessToken, result.ExpiresOn);
        }


        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
        {
            return GetTokenCoreAsync(requestContext, cancellationToken, false).GetAwaiter().GetResult();
        }

        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken = default)
        {
            return await GetTokenCoreAsync(requestContext, cancellationToken, true).ConfigureAwait(false);
        }

        private async ValueTask<AccessToken> GetTokenCoreAsync(TokenRequestContext requestContext, CancellationToken cancellationToken, bool async)
        {
            Exception inner = null;

            if (Record != null)
            {
                try
                {
                    var tenantId = _tenantId;
                    AuthenticationResult result = await Client.AcquireTokenSilent(requestContext.Scopes, new AuthenticationAccount(Record))
                            .WithTenantId(tenantId)
                            .WithClaims(requestContext.Claims)
                            .ExecuteAsync(cancellationToken)
                            .ConfigureAwait(false);

                    return new AccessToken(result.AccessToken, result.ExpiresOn);
                }
                catch (MsalUiRequiredException e)
                {
                    inner = e;
                }
            }

            if (DisableAutomaticAuthentication)
            {
                throw new AuthenticationRequiredException(AuthenticationRequiredMessage, requestContext, inner);
            }

            return await GetTokenViaBrowserLoginAsync(requestContext, cancellationToken, async: async).ConfigureAwait(false);
        }
    }
}
