
using Azure.Messaging.ServiceBus;
using System.Net;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using ServiceBusWriterAPI.Interfaces;

namespace ServiceBusWriterAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSingleton(_ => new ServiceBusClient(builder.Configuration["AZURE_SB_CONNECTION_STRING"]));
            builder.Services.AddSingleton<IMessageWriter, ServiceBusMessageWriter>();

            builder.Services.AddApplicationInsightsTelemetry();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            // Display Swagger UI for debugging purpose
            //if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
