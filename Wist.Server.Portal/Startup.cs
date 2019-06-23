using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Unity;
using Wist.Core.Architecture.UnityExtensions.Monitor;
using Wist.Core.Configuration;
using Wist.Server.Portal.Helpers;
using Wist.Server.Portal.Hubs;
using Wist.Server.Portal.Services;

namespace Wist.Server.Portal
{
    public class Startup
    {
        private readonly IUnityContainer _container;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public Startup(IConfiguration configuration, IUnityContainer unityContainer)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Configuration = configuration;
            _container = unityContainer;
            _container.AddNewExtension<MonitorUnityExtension>();
        }

        public IConfiguration Configuration { get; }

		public void ConfigureContainer(IUnityContainer container)
		{
			WebApiBootstrapper clientBootstrapper = new WebApiBootstrapper(_cancellationTokenSource.Token);
			clientBootstrapper.SetContainer(container);
			clientBootstrapper.ConfigureContainer();
			clientBootstrapper.ConfigureServiceLocator();
			AspAppConfig aspAppConfig = new AspAppConfig(Configuration);
			container.RegisterInstance<IAppConfig>(aspAppConfig);

			// configure DI for application services
			//container.RegisterSingleton<IGatewayService, GatewayService>();
			//container.RegisterSingleton<IAccountsService, AccountsService>();
			//container.RegisterSingleton<IExecutionContextManager, ExecutionContextManager>();

			clientBootstrapper.Initialize();
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });

            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            // configure jwt authentication
            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        bool isPortalUser = context.Principal.IsInRole("puser");

                        if (isPortalUser)
                        {
                            var userService = context.HttpContext.RequestServices.GetRequiredService<IAccountsService>();
                            var userId = ulong.Parse(context.Principal.Identity.Name);
                            //var user = userService.GetById(userId);
                            //if (user == null)
                            //{
                            //    // return unauthorized if user no longer exists
                            //    context.Fail("Unauthorized");
                            //}
                        }
                        return Task.CompletedTask;
                    }
                };

                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            //app.UseStaticFiles(new StaticFileOptions()
            //{
            //    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"Resources")),
            //    RequestPath = new PathString("/Resources")
            //});
            app.UseSpaStaticFiles();
            app.UseSignalR(s => s.MapHub<IdentitiesHub>("/identitiesHub", o => 
			{
				o.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling | Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
			}));

            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseAuthentication();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseAngularCliServer(npmScript: "start");
                }
            });
        }
    }
}
