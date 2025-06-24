using Duende.IdentityServer.Models;
using Duende.IdentityServer;

namespace IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("scope1"),
        };

    public static IEnumerable<Client> Clients(IConfiguration config)
    {
        var mcvClientId = config["IdentityServer:Clients:Mvc:ClientId"];
        var mvcClientSecret = config["IdentityServer:Clients:Mvc:ClientSecret"];

        if (string.IsNullOrEmpty(mcvClientId) || string.IsNullOrEmpty(mvcClientSecret))
        {
            throw new InvalidOperationException("Mvc client configuration is missing in the IdentityServer configuration.");
        }

        return
        [
            new Client
            {
                ClientId = mcvClientId,
                ClientSecrets = { new Secret(mvcClientSecret.Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://localhost:5002/signin-oidc" },
                PostLogoutRedirectUris = { "https://localhost:5002/signout-callback-oidc" },

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "scope1"
                }
            }
        ];
    }
}
