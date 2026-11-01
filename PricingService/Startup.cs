#nullable disable
using System.Linq;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using PricingService.DataAccess;
using PricingService.Domain;
using Steeltoe.Discovery.Client;

namespace PricingService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDiscoveryClient(Configuration);
            services.AddMvc().AddApplicationPart(typeof(Startup).Assembly).AddNewtonsoftJson();
            services.AddMediatR(opts => opts.RegisterServicesFromAssemblyContaining<Startup>());
            services.AddSingleton<IDataStore, InMemoryDataStore>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Loi validate (FluentValidation) -> HTTP 400
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (ValidationException ex)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";
                    var errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(errors));
                }
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}