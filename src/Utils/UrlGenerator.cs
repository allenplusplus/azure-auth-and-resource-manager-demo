using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using Microsoft.Extensions.Configuration;

namespace azure_auth_and_arm_demo
{
    public class UrlGenerator
    {
        private readonly string _provider;
        private readonly string _tenant;
        private readonly string _clientId;
        private readonly string _authUri;
        private readonly string _tokenUri;
        private readonly string _redirectUri;
        private readonly string _scope;
        private readonly string _clientSecret;
        private readonly string _resource;
        private readonly string _signoutUrl;

        private readonly string _getSubscriptionsUrl;
        private readonly string _getResourceGroupsUrl;
        private readonly string _getOrCreateOrUpdateDeploymentUrl;

        public UrlGenerator(IConfiguration configuration)
        {
            IConfigurationSection auth = configuration.GetSection("Auth");
            _provider = auth.GetValue<string>("Provider");
            _tenant = auth.GetValue<string>("TenantId");
            _clientId = auth.GetValue<string>("ClientId");
            _authUri = auth.GetValue<string>("AuthUri");
            _tokenUri = auth.GetValue<string>("TokenUri");
            _redirectUri = auth.GetValue<string>("RedirectUrl");
            _scope = auth.GetValue<string>("Scope");
            _resource = auth.GetValue<string>("Resource");
            _clientSecret = auth.GetValue<string>("ClientSecret");
            _signoutUrl = auth.GetValue<string>("SignoutUrl")
                + HttpUtility.UrlEncode(auth.GetValue<string>("SignoutRedirectUrl"));

            IConfigurationSection arm = configuration.GetSection("Arm");
            _getSubscriptionsUrl = arm.GetValue<string>("GetSubscriptionsUrl");
            _getResourceGroupsUrl = arm.GetValue<string>("GetResourceGroupsUrl");
            _getOrCreateOrUpdateDeploymentUrl = arm.GetValue<string>("GetOrCreateOrUpdateDeploymentUrl");
        }

        public string GenerateSignInUrl(string state)
        {
            return string.Format(_provider, _tenant) + _authUri + "?" + "client_id=" + _clientId
                + "&response_type=code&redirect_uri=" + HttpUtility.UrlEncode(_redirectUri) + "&scope="
                + HttpUtility.UrlEncode(_scope) + "&state=" + state;
        }

        public string GenerateTokenUrl()
        {
            return string.Format(_provider, _tenant) + _tokenUri;
        }

        public FormUrlEncodedContent GenerateTokenRequestContent(string code)
        {
            Dictionary<string, string> contentValues = new Dictionary<string, string>();
            contentValues.Add("client_id", _clientId);
            contentValues.Add("scope", _scope);
            contentValues.Add("code", code);
            contentValues.Add("redirect_uri", _redirectUri);
            contentValues.Add("grant_type", "authorization_code");
            contentValues.Add("client_secret", _clientSecret);
            contentValues.Add("resource", _resource);
            return new FormUrlEncodedContent(contentValues);
        }

        public string GenerateSubscriptionsUrl()
        {
            return _getSubscriptionsUrl;
        }

        public string GenerateResourceGroupsUrl(string subscriptionId)
        {
            return string.Format(_getResourceGroupsUrl, subscriptionId);
        }

        public string GenerateGetOrCreateOrUpdateDeploymentUrl(string subscriptionId, string resourceGroupName,
            string deploymentName)
        {
            return string.Format(_getOrCreateOrUpdateDeploymentUrl, subscriptionId, resourceGroupName, deploymentName);
        }

        public string GenerateSignoutUrl()
        {
            return _signoutUrl;
        }
    }
}