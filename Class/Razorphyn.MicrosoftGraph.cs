using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;


namespace OrderManager.Class
{

    public class GraphLogin
    {
        private string[] Scopes;

        private const string Tenant = "common";
        private const string Authority = "https://login.microsoftonline.com/" + Tenant;

        private static IPublicClientApplication PublicClientApp;

        private static string MSGraphURL = "https://graph.microsoft.com/v1.0/";
        private static AuthenticationResult authResult;


        public GraphLogin(string appId, string[] scopes)
        {
            Scopes = scopes;

            PublicClientApp = PublicClientApplicationBuilder.Create(appId)
                .WithAuthority(Authority)
                .WithBroker(true)
                .WithDefaultRedirectUri()
                 .WithLogging((level, message, containsPii) =>
                 {
                     Debug.WriteLine($"MSAL: {level} {message} ");
                 }, LogLevel.Warning, enablePiiLogging: false, enableDefaultPlatformLogging: true)
                .Build();
        }


        /// <summary>
        /// Call AcquireTokenAsync - to acquire a token requiring user to sign in
        /// </summary>
        internal async Task<GraphServiceClient> Authorize()
        {
            GraphServiceClient graphClient = null;

            try
            {
                graphClient = await SignInAndInitializeGraphServiceClient(Scopes);

                User graphUser = await graphClient.Me.GetAsync();

                string mess = "Display Name: " + graphUser.DisplayName + "\nBusiness Phone: " + graphUser.BusinessPhones.FirstOrDefault()
                                      + "\nGiven Name: " + graphUser.GivenName + "\nid: " + graphUser.Id
                                      + "\nUser Principal Name: " + graphUser.UserPrincipalName;

            }
            catch (MsalException msalEx)
            {
                await DisplayMessageAsync($"Error Acquiring Token:{Environment.NewLine}{msalEx}");
            }
            catch (Exception ex)
            {
                await DisplayMessageAsync($"Error Acquiring Token Silently:{Environment.NewLine}{ex}");
            }

            return graphClient;
        }

        internal async static Task<GraphServiceClient> SignInAndInitializeGraphServiceClient(string[] scopes)
        {
            var tokenProvider = new TokenProvider(SignInUserAndGetTokenUsingMSAL, scopes);
            var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);
            var graphClient = new GraphServiceClient(authProvider, MSGraphURL);

            return await Task.FromResult(graphClient);
        }

        /// <summary>
        /// Signs in the user and obtains an access token for Microsoft Graph
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns> Access Token</returns>
        internal static async Task<string> SignInUserAndGetTokenUsingMSAL(string[] scopes)
        {

            try
            {
                // It's good practice to not do work on the UI thread, so use ConfigureAwait(false) whenever possible.
                IEnumerable<IAccount> accounts = await PublicClientApp.GetAccountsAsync().ConfigureAwait(false);
                IAccount firstAccount = accounts.FirstOrDefault();

                authResult = await PublicClientApp.AcquireTokenSilent(scopes, firstAccount)
                                                  .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilentAsync. This indicates you need to call AcquireTokenAsync to acquire a token
                Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

                authResult = await PublicClientApp.AcquireTokenInteractive(scopes)
                                                  .WithParentActivityOrWindow(System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle)
                                                  .ExecuteAsync()
                                                  .ConfigureAwait(false);

            }

            return authResult.AccessToken;
        }

        internal async void SignOut()
        {
            IEnumerable<IAccount> accounts = await PublicClientApp.GetAccountsAsync().ConfigureAwait(false);
            IAccount firstAccount = accounts.FirstOrDefault();

            try
            {
                await PublicClientApp.RemoveAsync(firstAccount).ConfigureAwait(false);


            }
            catch (MsalException ex)
            {
                OnTopMessage.Error($"Error signing out user: {ex.Message}", "Microsoft Graph Login");
            }
        }

        internal static async Task DisplayMessageAsync(string message)
        {
            await Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal,
                () =>
                {
                    OnTopMessage.Default(message, "Microsoft Graph Login");
                });
        }

        public class TokenProvider : IAccessTokenProvider
        {
            private Func<string[], Task<string>> getTokenDelegate;
            private string[] scopes;

            public TokenProvider(Func<string[], Task<string>> getTokenDelegate, string[] scopes)
            {
                this.getTokenDelegate = getTokenDelegate;
                this.scopes = scopes;
            }

            public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> additionalAuthenticationContext = default,
                CancellationToken cancellationToken = default)
            {
                return getTokenDelegate(scopes);
            }

            public AllowedHostsValidator AllowedHostsValidator { get; }
        }
    }



    public class GraphUserHelper
    {
        public static async Task<string> GetTimezone(GraphServiceClient graphClient)
        {
            string timezone = "Europe/Berlin";

            try
            {
                var result = await graphClient.Me.MailboxSettings.GetAsync();

                timezone = result.TimeZone;
            }
            catch (MsalException ex)
            {
                Console.WriteLine($"Error getting signed-in user: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting signed-in user: {ex.Message}");
            }

            return timezone;
        }
    }
}


