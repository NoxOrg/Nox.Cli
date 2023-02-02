using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Nox.Cli.Server.Cache;
using Nox.Cli.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
builder.Services.AddWorkflowCache();
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
                AuthorizationUrl = new Uri("https://login.microsoftonline.com/88155c28-f750-4013-91d3-8347ddb3daa7/oauth2/v2.0/authorize"),
                TokenUrl = new Uri("https://login.microsoftonline.com/88155c28-f750-4013-91d3-8347ddb3daa7/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "api://750b96e1-e772-48f8-b6b3-84bac1961d9b/access_as_user", "Access web api on behalf of user" }
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
            new [] {"api://750b96e1-e772-48f8-b6b3-84bac1961d9b/access_as_user"}
        }  
    }); 
});

builder.Services.AddSingleton<ITaskExecutorFactory, TaskExecutorFactory>();

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
            options.OAuthClientId("750b96e1-e772-48f8-b6b3-84bac1961d9b");
            options.OAuthClientSecret("Sgt8Q~W0Sh0CdTeWRG0tXna10VINdDkK_B.Tkb2N");
            options.OAuthUseBasicAuthenticationWithAccessCodeGrant();
            options.OAuthScopes("api://750b96e1-e772-48f8-b6b3-84bac1961d9b/access_as_user");
        });    
    }
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();