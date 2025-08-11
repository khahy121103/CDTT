using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;


public class AccountController : Controller
{
    private static readonly string clientId = System.Configuration.ConfigurationManager.AppSettings["GoogleClientId"];
    private static readonly string clientSecret = System.Configuration.ConfigurationManager.AppSettings["GoogleClientSecret"];

    // Trang bấm đăng nhập Google
    public ActionResult LoginWithGoogle()
    {
        var authorizationUrl = GetGoogleAuthUrl();
        return Redirect(authorizationUrl);
    }

    // URL callback Google sẽ gọi về sau khi người dùng đăng nhập
    public async Task<ActionResult> GoogleCallback(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return RedirectToAction("Login", "Account");
        }

        var credential = await ExchangeCodeForTokenAsync(code);

        if (credential != null)
        {
            // Ở đây bạn có thể lấy thông tin user từ Google People API hoặc OpenId Connect
            return RedirectToAction("Index", "Home");
        }

        return RedirectToAction("Login", "Account");
    }

    // Tạo URL để redirect người dùng sang Google đăng nhập
    private string GetGoogleAuthUrl()
    {
        var scopes = new[] { "openid", "email", "profile" };

        var clientSecrets = new ClientSecrets
        {
            ClientId = clientId,
            ClientSecret = clientSecret
        };

        var redirectUri = Url.Action("GoogleCallback", "Account", null, Request.Url.Scheme);

        var authorizationUrl = GoogleOAuth2Helper.GetAuthorizationUrl(clientSecrets, scopes, redirectUri);

        return authorizationUrl;
    }

    // Đổi code trả về từ Google thành access token
    private async Task<UserCredential> ExchangeCodeForTokenAsync(string code)
    {
        var scopes = new[] { "openid", "email", "profile" };

        var clientSecrets = new ClientSecrets
        {
            ClientId = clientId,
            ClientSecret = clientSecret
        };

        var redirectUri = Url.Action("GoogleCallback", "Account", null, Request.Url.Scheme);

        return await GoogleOAuth2Helper.ExchangeCodeForTokenAsync(clientSecrets, scopes, code, redirectUri);
    }
}

public static class GoogleOAuth2Helper
{
    public static string GetAuthorizationUrl(ClientSecrets secrets, string[] scopes, string redirectUri)
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = secrets,
            Scopes = scopes
        });

        var request = flow.CreateAuthorizationCodeRequest(redirectUri);
        return request.Build().AbsoluteUri;
    }

    public static async Task<UserCredential> ExchangeCodeForTokenAsync(ClientSecrets secrets, string[] scopes, string code, string redirectUri)
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = secrets,
            Scopes = scopes,
            DataStore = new FileDataStore("GoogleTokenStore", true)
        });

        var token = await flow.ExchangeCodeForTokenAsync(
            userId: "user",
            code: code,
            redirectUri: redirectUri,
            taskCancellationToken: CancellationToken.None);

        return new UserCredential(flow, "user", token);
    }
}
