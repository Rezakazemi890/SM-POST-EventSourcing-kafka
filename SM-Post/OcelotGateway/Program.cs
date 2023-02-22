using Ocelot.DependencyInjection;
using Ocelot.Cache.CacheManager;
using System.Configuration;
using Microsoft.OpenApi.Models;
using Ocelot.Middleware;
using Ocelot.Values;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

//add ocelot
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration)
    .AddCacheManager(x =>
    {
        x.WithDictionaryHandle();
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
                },
                Scheme = "Bearer",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new string[] { }
        }
        });
    });
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseRouting();
app.UseSwagger();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});
app.UseStaticFiles();
app.UseSwaggerForOcelotUI(opt =>
{
    opt.DownstreamSwaggerHeaders = new[]
    {
                        new KeyValuePair<string, string>("Key", "Value"),
                        new KeyValuePair<string, string>("Key2", "Value2"),
                    };
}).UseOcelot().Wait();


//app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.Run();

