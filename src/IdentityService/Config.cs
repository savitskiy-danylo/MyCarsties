﻿using Duende.IdentityServer.Models;

namespace IdentityService;

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
            new ApiScope("auctionApp", "Auction app full access"),
      };

  public static IEnumerable<Client> Clients(IConfiguration config) =>
      new Client[]
      {
        new Client
        {
          ClientId = "nextApp",
          ClientName = "nextApp",
          AllowedScopes = {"openid", "profile", "auctionApp"},
          RedirectUris = {config["ClientApp"] + "/api/auth/callback/id-server"},
          ClientSecrets = {new Secret(config["ClientSecret"].Sha256())},
          AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
          RequirePkce = false,
          AllowOfflineAccess = true,
          AccessTokenLifetime = 36000*24*30,
          AlwaysIncludeUserClaimsInIdToken = true
        }
      };
}
