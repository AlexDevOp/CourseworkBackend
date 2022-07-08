using Microsoft.AspNetCore.Authentication.Negotiate;
using Newtonsoft.Json.Serialization;
using System.Security.Cryptography;
using Microsoft.OpenApi.Models;
using ÑourseworkBackend.CustomAttributes;
using ÑourseworkBackend.Middlewares;
using ÑourseworkBackend;

var builder = WebApplication.CreateBuilder(args);


//Console.WriteLine(System.Security.Principal.WindowsIdentity.GetCurrent().User.Value);

// Add services to the container.

builder.Services
    .AddControllers(options =>
    {
        options.InputFormatters.Insert(0, MyJPIF.GetJsonPatchInputFormatter());
    })
    .AddNewtonsoftJson(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());

builder.WebHost.UseUrls("http://*:5193;https://*:7193");

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("SessionToken", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization header using the api. Example: \"{token}\"",
        In = ParameterLocation.Header,
        Name = "SessionToken",
        Type = SecuritySchemeType.ApiKey
    });
    c.OperationFilter<SecurityRequirementsOperationFilter>();
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.UseMiddleware<SessionMiddleware>();

app.Run();

