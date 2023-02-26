using Ocelot.DependencyInjection;
using Ocelot.Cache.CacheManager;
using System.Configuration;
using Microsoft.OpenApi.Models;
using Ocelot.Middleware;
using Ocelot.Values;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

//add ocelot
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration)
    .AddCacheManager(x =>
    {
        x.WithDictionaryHandle();
    });


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});

builder.Services.AddSwaggerForOcelot(builder.Configuration, (swagger) =>
{
    swagger.GenerateDocsDocsForGatewayItSelf(opt =>
    {
        //This is to generate the Default UI of Swagger Documentation  
        opt.FilePathsForXmlComments = new string[] { "Gateway.xml" };
        opt.GatewayDocsTitle = "Gateway";
        opt.GatewayDocsOpenApiInfo = new()
        {
            Title = "Gateway",
            Version = "v1",
        };
        //opt.DocumentFilter<MyDocumentFilter>();
        // To Enable authorization using Swagger (JWT)  
        opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        });
        opt.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                          new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new string[] {}

                    }
                });
    });
});

#region For use identity Authentication
//Key
var key = Encoding.ASCII.GetBytes(builder.Configuration.GetSection("Jwt").GetRequiredSection("Key").Value);

// Add services to the container.

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            return Task.CompletedTask;
        }
    };
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = false,
        ValidIssuer = builder.Configuration.GetSection("Jwt").GetRequiredSection("Issuer").Value,
        //ValidAudience = builder.Configuration.GetSection("Jwt").GetRequiredSection("Issuer").Value,
    };
});
#endregion


var app = builder.Build();

// Configure the HTTP request pipeline.

// Configure the HTTP request pipeline.
app.UseStaticFiles();
app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});
app.UseAuthentication();
// Swagger Configuration in API  
app.UseSwagger();
app.UseSwaggerForOcelotUI(opt =>
{
    opt.DownstreamSwaggerHeaders = new[]
    {
                        new KeyValuePair<string, string>("Key", "Value"),
                        new KeyValuePair<string, string>("Key2", "Value2"),
                    };
}).UseOcelot().Wait();


app.Run();

