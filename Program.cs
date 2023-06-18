using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Graph;
using Microsoft.Graph.ExternalConnectors;
using Microsoft.Identity.Web;
using System.Globalization;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;
namespace CalendarIntegrationApi
{
    public class Program
    {

        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);
            var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            // Get the Token acquirer factory instance. By default it reads an appsettings.json
            // file if it exists in the same folder as the app (make sure that the 
            // "Copy to Output Directory" property of the appsettings.json file is "Copy if newer").
       /*     builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApp(configuration, "AzureAd")
          .EnableTokenAcquisitionToCallDownstreamApi(new string[] { "Calendars.Read","OnlineMeetingArtifact.Read.All","OnlineMeetings.Read.All","User.Read.All"}).AddMicrosoftGraph(configuration.GetSection("DownstreamApi"))
          .AddInMemoryTokenCaches();
            */
            builder.Services.AddMicrosoftIdentityWebApiAuthentication(configuration, "AzureAd")
                   .EnableTokenAcquisitionToCallDownstreamApi()
                       .AddMicrosoftGraph(configuration.GetSection("DownstreamApi"))
                       .AddInMemoryTokenCaches();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                //  app.UseSwagger();
                //  app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            //     app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}