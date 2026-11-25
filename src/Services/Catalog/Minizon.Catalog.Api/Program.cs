var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health")
    .WithMetadata(new HttpMethodMetadata([HttpMethods.Get]));

app.Run();
