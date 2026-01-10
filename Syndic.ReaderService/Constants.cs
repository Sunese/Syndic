namespace Syndic.ReaderService;

public static class Constants
{
  public const string CurrentUserKey = "CurrentUser";
  public const string AuthentikOidcSub = "Authentik";
  public const string GoogleOidcSub = "Google";
  public const string GithubOidcSub = "GitHub";
  public const string ProviderClaim = "provider";

  public const string SubClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
  public const string EmailClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
}
