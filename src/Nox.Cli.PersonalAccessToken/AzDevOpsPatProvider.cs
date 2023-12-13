using Newtonsoft.Json;
using Nox.Cli.Abstractions.Caching;
using Nox.Cli.Abstractions.Exceptions;
using RestSharp;
using RestSharp.Authenticators.OAuth2;

namespace Nox.Cli.PersonalAccessToken;

public class AzDevOpsPatProvider
{
    private readonly string _organization;
    private readonly IPersistedTokenCache _tokenCache;
    
    public AzDevOpsPatProvider(
        IPersistedTokenCache tokenCache,
        string organization)
    {
        _tokenCache = tokenCache;
        _organization = organization;
    }

    public async Task<string> GetPat(string accessToken)
    {
        var result = "";
        var cachedPat = await _tokenCache.LoadAsync("MyDevopsPat");

        if (!string.IsNullOrEmpty(cachedPat))
        {
            var pat = JsonConvert.DeserializeObject<AzDevOpsPat>(cachedPat);
            if (pat!.ValidTo < DateTime.Now)
            {
                pat = await GetOrCreateOnlinePat(accessToken);
            }

            result = pat.Token!;
        }
        else
        {
            var pat = await GetOrCreateOnlinePat(accessToken);
            await _tokenCache.SaveAsync("MyDevopsPat", JsonConvert.SerializeObject(pat));
            result = pat.Token!;
        }

        return result;
    }

    private async Task<AzDevOpsPat> GetOrCreateOnlinePat(string accessToken)
    {
        //check that the pat exists in the cloud and has not expired
        var onlinePats = await GetOnlinePatList(accessToken);
        if (onlinePats != null)
        {
            var existingPat = onlinePats.PatTokens!.FirstOrDefault(p => p.DisplayName!.Equals("nox.cli", StringComparison.OrdinalIgnoreCase));
            if (existingPat != null)
            {
                await RevokeOnlinePat(accessToken, existingPat.AuthorizationId!.Value);
            }

            return await NewOnlinePat(accessToken);
        }
        else
        {
            throw new NoxCliException("Unable to fetch a list of your Azure DevOps Personal access tokens, are you connected to the internet?");
        }
    }

    private async Task<AzDevOpsPatList?> GetOnlinePatList(string accessToken)
    {
        var client = new RestClient($"https://vssps.dev.azure.com/{_organization}/_apis/tokens/pats?api-version=7.1-preview.1", options =>
        {
            options.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(accessToken, "Bearer");
        });
        var request = new RestRequest() { Method = Method.Get };
        var response = await client.ExecuteAsync(request);
        return JsonConvert.DeserializeObject<AzDevOpsPatList>(response.Content!);
    }
        

    private async Task<AzDevOpsPat> NewOnlinePat(string accessToken)
    {
        var client = new RestClient($"https://vssps.dev.azure.com/{_organization}/_apis/tokens/pats?api-version=7.1-preview.1", options =>
        {
            options.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(accessToken, "Bearer");    
        });

        var validToDate = DateTime.Now.AddDays(30);
        var request = new RestRequest() { Method = Method.Post };
        request.AddJsonBody($"{{\"displayName\":\"Nox.Cli\", \"scope\":\"app_token\", \"validTo\":\"{validToDate}\", \"allOrgs\":\"false\"}}");
        request.AddHeader("Accept", "application/json");
        var response = await client.ExecuteAsync(request);
        var patResponse = JsonConvert.DeserializeObject<AzDevOpsPatResponse>(response.Content!);
        return patResponse!.PatToken!;
    }

    private async Task RevokeOnlinePat(string accessToken, Guid authorizationId)
    {
        var result = new AzDevOpsPat();
        var client = new RestClient($"https://vssps.dev.azure.com/{_organization}/_apis/tokens/pats?api-version=7.1-preview.1&authorizationId={authorizationId}", options =>
        {
            options.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(accessToken, "Bearer");    
        });

        var request = new RestRequest() { Method = Method.Delete };
        request.AddHeader("Accept", "application/json");
        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful) throw new NoxCliException(response.StatusDescription!);
    }
}