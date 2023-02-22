using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Post.Authorization.Domain.Entities;
using Post.Authorization.Infrastructure.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Post.Authorization.Domain.Repositories;
using Post.Authorization.Infrastructure.Repositories;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

//DbContext and connection string
var connectionString = builder.Configuration.GetConnectionString("AuthorizerConnectionString");

builder.Services.AddDbContext<AuthorizationDbContext>(options =>
    options.UseLazyLoadingProxies().UseSqlServer(connectionString));

builder.Services.AddSingleton<AuthorizationDbContextFactory>(new AuthorizationDbContextFactory(options =>
    options.UseLazyLoadingProxies().UseSqlServer(connectionString)));

builder.Services.AddIdentityCore<PostUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<AuthorizationDbContext>();


//Create Database and Tables From Code
var dataContext = builder.Services.BuildServiceProvider().GetRequiredService<AuthorizationDbContext>();
dataContext.Database.EnsureCreated();

// Add services to the container.
builder.Services.AddScoped<IPostUserRepository, PostUserRepository>();

builder.Services.AddControllers();
builder.Services.AddMvc();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(swagger =>
{
    //This is to generate the Default UI of Swagger Documentation  
    swagger.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Authorization Api",
        Description = "Post Authorization Api"
    });
    // To Enable authorization using Swagger (JWT)  
    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
    });
    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
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
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidIssuer = builder.Configuration.GetSection("Jwt").GetRequiredSection("Issuer").Value,
        ValidAudience = builder.Configuration.GetSection("Jwt").GetRequiredSection("Issuer").Value,
    };
});
#endregion

#region serilog config

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .Build();

ConfigureLogging(environment, configuration);


static void ConfigureLogging(string environment, IConfigurationRoot configuration)
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .WriteTo.Console()
        .WriteTo.Debug()
        .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(configuration["ElasticSearch:Uri"]))
        {
            AutoRegisterTemplate = true,
            AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6,
            IndexFormat = $"{Assembly.GetExecutingAssembly().GetName().Name!.ToLower().Replace(".", "-")}-{environment?.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}"
        })
        .ReadFrom.Configuration(configuration)
        .CreateLogger();
}

//For Elastic Old

//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Debug()
//    .WriteTo.Console(theme: SystemConsoleTheme.Literate)
//    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(builder.Configuration.GetConnectionString("elasticsearch"))) // for the docker-compose implementation
//    {
//        AutoRegisterTemplate = true,
//        OverwriteTemplate = true,
//        DetectElasticsearchVersion = true,
//        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
//        NumberOfReplicas = 1,
//        NumberOfShards = 2,
//        //BufferBaseFilename = "./buffer",
//        //RegisterTemplateFailure = RegisterTemplateRecovery.FailSink,
//        FailureCallback = e => Console.WriteLine("Unable to submit event " + e.MessageTemplate),
//        EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
//                           EmitEventFailureHandling.WriteToFailureSink |
//                           EmitEventFailureHandling.RaiseCallback,
//        FailureSink = new FileSink("./fail-{Date}.txt", new JsonFormatter(), null, null)
//    })
//    .CreateLogger();



//with configuration

//IConfigurationRoot? seriLogConfig = null;
//if (builder.Environment.IsDevelopment())
//{
//    seriLogConfig = builder.Configuration.AddJsonFile("appsettings.Development.json").Build();
//}
//else
//{
//    seriLogConfig = builder.Configuration.AddJsonFile("appsettings.json").Build();
//}

//Log.Logger = new LoggerConfiguration()
//    .ReadFrom.Configuration(seriLogConfig)
//    .CreateLogger();


//var columnOpt = new Serilog.Sinks.MSSqlServer.ColumnOptions();
//columnOpt.Store.Add(Serilog.Sinks.MSSqlServer.StandardColumn.LogEvent);
//columnOpt.AdditionalColumns = new Collection<SqlColumn>{
//    new SqlColumn
//    {
//        ColumnName = "RequestUri",
//        AllowNull = true,
//        DataType = System.Data.SqlDbType.NVarChar,
//        DataLength = 2048,
//        PropertyName = "Uri"
//    }
//};

//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Warning()
//    .WriteTo.MSSqlServer(
//        connectionString: builder.Configuration.GetConnectionString("SqlServer"),
//        sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions { TableName = "LogEvent", AutoCreateSqlTable = true },
//        columnOptions:columnOpt
//    )
//    .CreateLogger();

builder.Host.UseSerilog();

#endregion

#region For basic JWT Authentication
//builder.Services.AddAuthentication(option =>
//{
//    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

//}).AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = false,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = builder.Configuration.GetSection("Jwt").GetRequiredSection("Issuer").Value,
//        ValidAudience = builder.Configuration.GetSection("Jwt").GetRequiredSection("Issuer").Value,
//        IssuerSigningKey = new SymmetricSecurityKey(
//            Encoding.UTF8.GetBytes(
//                builder.Configuration.GetSection("Jwt").GetRequiredSection("Key").Value
//                )
//            ) //Configuration["JwtToken:SecretKey"]  
//    };
//});
#endregion

var app = builder.Build();

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
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Authorization.Api");

});

app.Run();

