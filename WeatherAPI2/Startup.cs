using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OpenApi.Models;
using WeatherAPI2.Controllers;

namespace WeatherAPI2
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
            services.AddOData();
            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            });

            SetOutputFormatters(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.EnableDependencyInjection();
                endpoints.Select().Filter().Expand().MaxTop(10);

                //default way ok
                // endpoints.MapODataRoute("odata", "odata", GetEdmModel());

                //version 1 ok
                // CustomMapODataRouteV1(endpoints, "odata", "odata", GetEdmModel());

                //version 2 does not work
                //http://localhost:5000/odata/get42() returns 404 not found
                CustomMapODataRouteV2(endpoints, "odata", "odata", GetEdmModel());
            });

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
        }

        public static IEndpointRouteBuilder CustomMapODataRouteV1(IEndpointRouteBuilder builder, string routeName, string routePrefix, IEdmModel model)
        {
            return builder.MapODataRoute(routeName, routePrefix, delegate (IContainerBuilder containerBuilder)
            {
                IContainerBuilder builder2 = containerBuilder.AddService(Microsoft.OData.ServiceLifetime.Singleton, ((IServiceProvider sp) => model));
                Func<IServiceProvider, IEnumerable<IODataRoutingConvention>> magicFunc = ((IServiceProvider sp) => ODataRoutingConventions.CreateDefaultWithAttributeRouting(routeName, builder.ServiceProvider));
                builder2.AddService(Microsoft.OData.ServiceLifetime.Singleton, magicFunc);
            });
        }

        public static IEndpointRouteBuilder CustomMapODataRouteV2(IEndpointRouteBuilder builder, string routeName, string routePrefix, IEdmModel model)
        {
            return builder.MapODataRoute(routeName, routePrefix, delegate (IContainerBuilder containerBuilder)
            {
                IContainerBuilder builder2 = containerBuilder.AddService(Microsoft.OData.ServiceLifetime.Singleton, ((IServiceProvider sp) => model));
                builder2.AddService(Microsoft.OData.ServiceLifetime.Singleton, ((IServiceProvider sp) => ODataRoutingConventions.CreateDefaultWithAttributeRouting(routeName, builder.ServiceProvider)));
            });
        }


        IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<WeatherForecast>("WeatherForecast");
            builder.Function(nameof(WeatherForecastController.Get42)).Returns<int>();
            return builder.GetEdmModel();
        }

        private static void SetOutputFormatters(IServiceCollection services)
        {
            services.AddMvcCore(options =>
            {
                IEnumerable<ODataOutputFormatter> outputFormatters =
                    options.OutputFormatters.OfType<ODataOutputFormatter>()
                        .Where(foramtter => foramtter.SupportedMediaTypes.Count == 0);

                foreach (var outputFormatter in outputFormatters)
                {
                    outputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/odata"));
                }
            });
        }
    }
}
