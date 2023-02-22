using System.Collections.ObjectModel;
using System.Reflection;
using Confluent.Kafka;
using CQRS.Core.Consumers;
using CQRS.Core.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Post.Query.Api.Queries;
using Post.Query.Domain.Entities;
using Post.Query.Domain.Repositories;
using Post.Query.Infrastructure.Consumers;
using Post.Query.Infrastructure.DataAccess;
using Post.Query.Infrastructure.Dispatchers;
using Post.Query.Infrastructure.Handlers;
using Post.Query.Infrastructure.Repositories;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.File;
using Serilog.Sinks.MSSqlServer;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
Action<DbContextOptionsBuilder> configureDbContext;
var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

if (env.Equals("Development.PostgreSQL"))
{
    configureDbContext =
        (o => o.UseLazyLoadingProxies().UseNpgsql(builder.Configuration.GetConnectionString("SqlServer")));
}
else
{
    configureDbContext =
        (o => o.UseLazyLoadingProxies().UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
}

builder.Services.AddDbContext<DatabaseContext>(configureDbContext);
builder.Services.AddSingleton<DatabaseContextFactory>(new DatabaseContextFactory(configureDbContext));

//Create Database and Tables From Code
var dataContext = builder.Services.BuildServiceProvider().GetRequiredService<DatabaseContext>();
dataContext.Database.EnsureCreated();

builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IQueryHandler, QueryHandler>();
builder.Services.AddScoped<IEventHandler, Post.Query.Infrastructure.Handlers.EventHandler>();
builder.Services.Configure<ConsumerConfig>(builder.Configuration.GetSection(nameof(ConsumerConfig)));
builder.Services.AddScoped<IEventConsumer, EventConsumer>();

//Register QueryHandler Methods

var queryHandler = builder.Services.BuildServiceProvider().GetRequiredService<IQueryHandler>();
var dispatcher = new QueryDispatcher();

dispatcher.RegisterHandler<FindAllPostQuery>(queryHandler.HandleAsync);
dispatcher.RegisterHandler<FindPostByIdQuery>(queryHandler.HandleAsync);
dispatcher.RegisterHandler<FindPostWithAuthorQuery>(queryHandler.HandleAsync);
dispatcher.RegisterHandler<FindPostWithCommentsQuery>(queryHandler.HandleAsync);
dispatcher.RegisterHandler<FindPostWithLikesQuery>(queryHandler.HandleAsync);

builder.Services.AddSingleton<IQueryDispatcher<PostEntity>>(_ => dispatcher);

builder.Services.AddControllers();

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

builder.Services.AddHostedService<ConsumerHostedService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseStaticFiles();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
