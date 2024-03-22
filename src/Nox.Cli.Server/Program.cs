using Elastic.Apm.NetCoreAll;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Nox.Cli.Caching;
using Nox.Cli.Server.Abstractions;
using Nox.Cli.Server.Caching;
using Nox.Cli.Server.Services;
using Nox.Cli.Variables.Secrets;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, subscribeToJwtBearerMiddlewareDiagnosticsEvents: true);

var cacheManager = new NoxCliCacheBuilder(builder.Configuration["NoxScriptsUrl"]!, false)
    .WithTenantId(builder.Configuration["AzureAd:TenantId"]!)
    .ForServer()
    .Build();

builder.Services
    .AddWorkflowCache()
    .AddNoxCliCacheManager(cacheManager)
    .AddPersistedSecretStore()
    .AddServerSecretResolver(builder.Configuration["ServerSecretResolver:TenantId"]!, builder.Configuration["ServerSecretResolver:ClientId"]!, builder.Configuration["ServerSecretResolver:ClientSecret"]!);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var secScheme = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{builder.Configuration["AzureAd:Instance"]}/{builder.Configuration["AzureAd:TenantId"]}/oauth2/v2.0/authorize"),
                TokenUrl = new Uri($"{builder.Configuration["AzureAd:Instance"]}/{builder.Configuration["AzureAd:TenantId"]}/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string>
                {
                    { $"api://{builder.Configuration["AzureAd:ClientId"]}/{builder.Configuration["AzureAd:Scopes"]}", "Access web api on behalf of user" }
                }
            }
        },
        Reference = new OpenApiReference {  
            Type = ReferenceType.SecurityScheme,  
            Id = "oauth2"  
        },  
        Scheme = "oauth2",  
        Name = "oauth2",  
        In = ParameterLocation.Header  
    };
    
    options.AddSecurityDefinition("oauth2", secScheme);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement {  
        {
            secScheme,
            new [] {$"api://{builder.Configuration["AzureAd:ClientId"]}/access_as_user"}
        }  
    }); 
});

builder.Services.AddSingleton<IWorkflowContextFactory, WorkflowContextFactory>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.OAuthAppName("SwaggerClient");
            options.OAuthClientId(builder.Configuration["AzureAd:ClientId"]);
            options.OAuthClientSecret("Sgt8Q~W0Sh0CdTeWRG0tXna10VINdDkK_B.Tkb2N");
            options.OAuthUseBasicAuthenticationWithAccessCodeGrant();
            options.OAuthScopes($"api://{builder.Configuration["AzureAd:ClientId"]}/{builder.Configuration["AzureAd:Scopes"]}");
        });    
    }
}
app.UseAllElasticApm(builder.Configuration);
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();