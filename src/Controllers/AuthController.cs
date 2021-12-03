using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using azure_auth_and_arm_demo.Caches;
using azure_auth_and_arm_demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace azure_auth_and_arm_demo.Controllers
{
    public class AuthController: Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly HttpClient _httpClient;
        private readonly UrlGenerator _urlGenerator;
        private readonly StateCache _stateCache;
        private readonly SessionCache _sessionCache;

        public AuthController(ILogger<AuthController> logger, IHttpClientFactory _httpClientFactory,
            UrlGenerator urlGenerator, SessionCache sessionCache, StateCache stateCache)
        {
            _logger = logger;
            _httpClient = _httpClientFactory.CreateClient();
            _urlGenerator = urlGenerator;
            _stateCache = stateCache;
            _sessionCache = sessionCache;
        }

        [Route("/auth/signin")]
        public IActionResult Initiate()
        {
            string state = Guid.NewGuid().ToString();
            string signInUrl = _urlGenerator.GenerateSignInUrl(state);
            _logger.LogInformation("state=" + state);
            _logger.LogInformation("signInUrl=" + signInUrl);

            _stateCache.CreateEntry(state, state);

            string sessionId = Guid.NewGuid().ToString();
            Response.Cookies.Delete("azure_auth_and_arm_demo_session_id");
            Response.Cookies.Append("azure_auth_and_arm_demo_session_id", sessionId);

            _sessionCache.CreateEntry(sessionId, new Session());

            _logger.LogInformation("created session " + sessionId);

            return Redirect(signInUrl);
        }

        [Route("/auth/callback")]
        public async Task<IActionResult> Callback(string code, string state)
        {
            string sessionId = Request.Cookies["azure_auth_and_arm_demo_session_id"];
            if (sessionId == null || sessionId.Length == 0)
            {
                _logger.LogInformation("no session cookie");
                return Unauthorized("unauthorized");
            }

            _logger.LogInformation("sessionid=" + sessionId);

            Session session;
            bool sessionFound = _sessionCache.TryGetValue(sessionId, out session);
            if (!sessionFound) {
                _logger.LogInformation("no session");
                return Unauthorized("unauthorized");
            }

            if (session.Token != null
                && session.Token.ExpiresOn < DateTimeOffset.Now.ToUnixTimeSeconds())
            {
                _logger.LogInformation("session has valid token");
                return Redirect("/");
            }

            _logger.LogInformation("code=" + code);
            _logger.LogInformation("state=" + state);

            string cachedState;
            bool found = _stateCache.TryGetValue(state, out cachedState);
            if (!found)
            {
                _logger.LogInformation("invalid state");
            }

            _logger.LogInformation("valid state");

            _stateCache.Remove(cachedState);

            string tokenUrl = _urlGenerator.GenerateTokenUrl();
            FormUrlEncodedContent tokenRequestContent = _urlGenerator.GenerateTokenRequestContent(code);

            string tokenRequestContentString = await tokenRequestContent.ReadAsStringAsync();
            _logger.LogInformation("tokenRequestContent=" + tokenRequestContentString);

            _logger.LogInformation("tokenUrl=" + tokenUrl);

            string tokenContent;
            using (HttpResponseMessage tokenResponse = await _httpClient.PostAsync(tokenUrl, tokenRequestContent))
            {
                _logger.LogInformation("statusCode=" + tokenResponse.StatusCode.ToString());

                if (tokenResponse.StatusCode != HttpStatusCode.OK)
                {
                    return StatusCode(500);
                }

                tokenContent = await tokenResponse.Content.ReadAsStringAsync();

                _logger.LogInformation("token=" + tokenContent);
            }

            Token token = JsonSerializer.Deserialize<Token>(tokenContent,
                new JsonSerializerOptions()
                {
                    NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString
                }
            );

            _logger.LogInformation("token expiresIn=" + token.ExpiresOn);
            _logger.LogInformation("currentTime=" + DateTimeOffset.Now.ToUnixTimeSeconds());

            session.Token = token;

            return Redirect("/");
        }

        [Route("/auth/signout")]
        public IActionResult Signout()
        {
            string sessionId = Request.Cookies["azure_auth_and_arm_demo_session_id"];
            _sessionCache.Remove(sessionId);

            return Redirect("/");
        }
    }
}