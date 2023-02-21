using System.Collections.ObjectModel;
using System.Configuration;
using System.Reflection;
using Confluent.Kafka;
using CQRS.Core.Domain;
using CQRS.Core.Events;
using CQRS.Core.Handlers;
using CQRS.Core.Infrastructure;
using CQRS.Core.Producers;
using Microsoft.AspNetCore.Hosting;
using MongoDB.Bson.Serialization;
using Post.Cmd.Api.Commands;
using Post.Cmd.Domain.Aggregates;
using Post.Cmd.Infrastructure.Config;
using Post.Cmd.Infrastructure.Dispatchers;
using Post.Cmd.Infrastructure.Handlers;
using Post.Cmd.Infrastructure.Producers;
using Post.Cmd.Infrastructure.Repositories;
using Post.Cmd.Infrastructure.Stores;
using Post.Common.Events;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.File;
using Serilog.Sinks.MSSqlServer;
using Serilog.Sinks.SystemConsole.Themes;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

BsonClassMap.RegisterClassMap<BaseEvent>();
BsonClassMap.RegisterClassMap<PostCreatedEvent>();
BsonClassMap.RegisterClassMap<MessageUpdatedEvent>();
BsonClassMap.RegisterClassMap<PostLikedEvent>();
BsonClassMap.RegisterClassMap<CommentAddedEvent>();
BsonClassMap.RegisterClassMap<CommentUpdatedEvent>();
BsonClassMap.RegisterClassMap<CommentRemovedEvent>();
BsonClassMap.RegisterClassMap<PostRemovedEvent>();


// Add services to the container.
builder.Services.Configure<MongoDbConfig>(builder.Configuration.GetSection(nameof(MongoDbConfig)));
builder.Services.Configure<ProducerConfig>(builder.Configuration.GetSection(nameof(ProducerConfig)));

builder.Services.AddScoped<IEventStoreRepository, EventStoreRepository>();
builder.Services.AddScoped<IEventStore, EventStore>();
builder.Services.AddScoped<IEventSourcingHandler<PostAggregate>, EventSourcingHandler>();
builder.Services.AddScoped<ICommandHandler, CommandHandler>();
builder.Services.AddScoped<IEventProducer, EventProducer>();

//Register Command Handler Methods
var commandHandler = builder.Services.BuildServiceProvider().GetRequiredService<ICommandHandler>();
var commandDispatcher = new CommandDispatcher();

commandDispatcher.RegisterHandler<NewPostCommand>(commandHandler.HandleAsync);
commandDispatcher.RegisterHandler<EditMessageCommand>(commandHandler.HandleAsync);
commandDispatcher.RegisterHandler<EditCommentCommand>(commandHandler.HandleAsync);
commandDispatcher.RegisterHandler<LikePostCommand>(commandHandler.HandleAsync);
commandDispatcher.RegisterHandler<RemoveCommentCommand>(commandHandler.HandleAsync);
commandDispatcher.RegisterHandler<DeletePostCommand>(commandHandler.HandleAsync);
commandDispatcher.RegisterHandler<RestoreReadDbCommand>(commandHandler.HandleAsync);
commandDispatcher.RegisterHandler<AddCommentCommand>(commandHandler.HandleAsync);

builder.Services.AddSingleton<ICommandDispatcher>(_ => commandDispatcher);

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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
