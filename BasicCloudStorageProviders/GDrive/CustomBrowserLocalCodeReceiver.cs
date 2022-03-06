using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LCR = Google.Apis.Auth.OAuth2.LocalServerCodeReceiver;

namespace Encryptor.Lib.GDrive
{
    //slightly modernised fork from official Google Drive library with its dependent types
    public class CustomBrowserLocalCodeReceiver : ICodeReceiver
    {
        static CustomBrowserLocalCodeReceiver()
        {
            _callbackUriChooser =
                typeof(Google.Apis.Auth.OAuth2.LocalServerCodeReceiver).Assembly.GetType("Google.Apis.Auth.OAuth2.LocalServerCodeReceiver+CallbackUriChooser")
                    .GetProperty("Default").GetValue(null);
            _cucReportSuccess = _callbackUriChooser.GetType().GetMethod("ReportSuccess");
            _cucReportFailure = _callbackUriChooser.GetType().GetMethod("ReportFailure");
        }

        /// <summary>
        /// Create an instance of <see cref="CustomBrowserLocalCodeReceiver"/>.
        /// </summary>
        public CustomBrowserLocalCodeReceiver() : this(DefaultClosePageResponse, LCR.CallbackUriChooserStrategy.Default) { }

        /// <summary>
        /// Create an instance of <see cref="CustomBrowserLocalCodeReceiver"/>.
        /// </summary>
        /// <param name="closePageResponse">Custom close page response for this instance</param>
        public CustomBrowserLocalCodeReceiver(string closePageResponse) :
            this(closePageResponse, LCR.CallbackUriChooserStrategy.Default)
        { }

        /// <summary>
        /// Create an instance of <see cref="CustomBrowserLocalCodeReceiver"/>.
        /// </summary>
        /// <param name="closePageResponse">Custom close page response for this instance</param>
        /// <param name="strategy">The strategy to use to determine the callback URI</param>
        public CustomBrowserLocalCodeReceiver(string closePageResponse, LCR.CallbackUriChooserStrategy strategy)
        {
            _closePageResponse = closePageResponse;
            // Set the instance field of which callback URI to use.
            // An instance field is used to ensure any one instance of this class
            // uses a consistent callback URI.



            _callbackUriTemplate = (string)(_callbackUriChooser.GetType().GetMethod("GetUriTemplate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                        .Invoke(_callbackUriChooser, new object[] { strategy }));
        }


        private static readonly object _callbackUriChooser;
        private static readonly System.Reflection.MethodInfo _cucReportSuccess;
        private static readonly System.Reflection.MethodInfo _cucReportFailure;

        // Callback URI used for this instance.
        private string _callbackUriTemplate;

        // Close page response for this instance.
        private readonly string _closePageResponse;


        /// <summary>The call back request path.</summary>
        internal const string LoopbackCallbackPath = "/authorize/";

        /// <summary>Close HTML tag to return the browser so it will close itself.</summary>
        internal const string DefaultClosePageResponse =
@"<html>
  <head><title>OAuth 2.0 Authentication Token Received</title></head>
  <body>
    Received verification code. You may now close this window.
    <script type='text/javascript'>
      // This doesn't work on every browser.
      window.setTimeout(function() {
          this.focus();
          window.opener = this;
          window.open('', '_self', ''); 
          window.close(); 
        }, 1000);
      //if (window.opener) { window.opener.checkToken(); }
    </script>
  </body>
</html>";



        /// <summary>Returns a random, unused port.</summary>
        private static int _getRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        private HttpListener _startListener()
        {
            try
            {
                var listener = new HttpListener();
                listener.Prefixes.Add(RedirectUri);
                listener.Start();
                return listener;
            }
            catch
            {
                //CallbackUriChooser.Default.ReportFailure(_callbackUriTemplate);
                _cucReportFailure.Invoke(_callbackUriChooser, new object[] { _callbackUriTemplate });
                throw;
            }
        }


        private async Task<AuthorizationCodeResponseUrl> _getResponseFromListener(HttpListener listener, CancellationToken ct)
        {
            HttpListenerContext context;
            // Set up cancellation. HttpListener.GetContextAsync() doesn't accept a cancellation token,
            // the HttpListener needs to be stopped which immediately aborts the GetContextAsync() call.
            using (ct.Register(listener.Stop))
            {
                // Wait to get the authorization code response.
                try
                {
                    context = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception) when (ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                    // Next line will never be reached because cancellation will always have been requested in this catch block.
                    // But it's required to satisfy compiler.
                    throw new InvalidOperationException();
                }
                catch
                {
                    //CallbackUriChooser.Default.ReportFailure(_callbackUriTemplate);
                    _cucReportFailure.Invoke(_callbackUriChooser, new object[] { _callbackUriTemplate });
                    throw;
                }
                //CallbackUriChooser.Default.ReportSuccess(_callbackUriTemplate);
                _cucReportSuccess.Invoke(_callbackUriChooser, new object[] { _callbackUriTemplate });
            }
            NameValueCollection coll = context.Request.QueryString;

            // Write a "close" response.
            var bytes = Encoding.UTF8.GetBytes(_closePageResponse);
            context.Response.ContentLength64 = bytes.Length;
            context.Response.SendChunked = false;
            context.Response.KeepAlive = false;
            var output = context.Response.OutputStream;
            await output.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            await output.FlushAsync().ConfigureAwait(false);
            output.Close();
            context.Response.Close();

            // Create a new response URL with a dictionary that contains all the response query parameters.
            return new AuthorizationCodeResponseUrl(coll.AllKeys.ToDictionary(k => k, k => coll[k]));
        }







        private string redirectUri;
        /// <inheritdoc />
        public string RedirectUri
        {
            get
            {
                if (string.IsNullOrEmpty(redirectUri))
                {
                    redirectUri = string.Format(_callbackUriTemplate, _getRandomUnusedPort());
                }
                return redirectUri;
            }
        }

        public async Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(AuthorizationCodeRequestUrl url, CancellationToken taskCancellationToken)
        {
            var authorizationUrl = url.Build();
            using var listener = _startListener();

            LaunchBrowser(this, new OpenBroswerEventArgs(authorizationUrl));

            var ret = await _getResponseFromListener(listener, taskCancellationToken).ConfigureAwait(false);

            return ret;
        }


        public event EventHandler<OpenBroswerEventArgs> LaunchBrowser;

    }
}
