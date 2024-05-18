using AspNetCore.ReCaptcha;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using TestLinux.Middleware;
using TestLinux.Models;

namespace TestLinux
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            
            string mySqlConnectionStr = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContextPool<DatabaseContext>(options => options.UseMySql(mySqlConnectionStr, ServerVersion.AutoDetect(mySqlConnectionStr)));
            services.AddAntiforgery(options =>
            {
                options.Cookie.Name = "crgb_cgbank";
                //options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });
            services.AddControllersWithViews();
            services.AddHttpContextAccessor();
            services.AddTokenAuthentication(Configuration);

            //--------------Added on 09-06-2022 (To force the client browsers to use HTTPS )----
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
                //options.ExcludedHosts.Add("cgbank.in");
                //options.ExcludedHosts.Add("www.cgbank.in");
            });

            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = (int)HttpStatusCode.TemporaryRedirect;
                options.HttpsPort = 5000;
            });
            //-------------------------------------------------

            //--------------Added on 09-06-2022 (AspNetCoreRateLimit Nuget for RateLimitings)----
            services.AddOptions();
            services.AddMemoryCache();
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimitPolicies"));
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
            services.AddInMemoryRateLimiting();
            services.AddReCaptcha(Configuration.GetSection("ReCaptcha"));

            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.Cookie.Name = "crgb";
                options.IdleTimeout = TimeSpan.FromSeconds(300);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            
            services.AddSingleton<UniqueCode>();
            services.AddSingleton<CustomIDataProtection>();
            //----Added on 15-05-2024----
            //services.AddCors(options => options.AddDefaultPolicy( builder => builder.WithOrigins("https://cdn.datatables.net")));
            services.AddCors(options => options.AddDefaultPolicy(builder => builder.AllowAnyOrigin()));
            //-------------------------------------------------
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStaticFiles();
            app.UseExceptionHandler("/Home/Error");
             
            //----Added on 09-06-2022(X-Xss-Protection Enabled)-------
            app.Use(async (context, next) =>
            {
                //------------------For X-Xss Protection----------------------
                context.Response.Headers.Add("X-Xss-Protection", "1");
                //------------------For X-Frame-Options DENY/SAMEORIGIN clickJacking----------------------
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                //context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");

                //------------------For X-Content-Type-Options to avoid MIME Sniffing----------------------
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                //------------------For Content-Security-Policy added on 08-05-2024 ----------------------
                
                context.Response.Headers.Add("Content-Security-Policy", "default-src 'self' https:; connect-src 'self' https: wss: ws: http://localhost:50850; script-src 'self' 'unsafe-inline' https://cdn.datatables.net https://cdnjs.cloudflare.com https://code.jquery.com/ https://unpkg.com/ https://www.google.com/ https://maps.googleapis.com/ https://www.gstatic.com/; style-src 'self' 'unsafe-inline' https://cdn.datatables.net https://cdnjs.cloudflare.com; img-src 'self' data:; font-src 'self' https://cdn.datatables.net https://cdnjs.cloudflare.com data:;");
                //------------------For server name disclosure removed server header added on 08 - 05 - 2024----------------------
                context.Response.Headers.Remove("Server");
                await next();
            });
            //----Added on 09-06-2022(mark cookie flag as secure always)-------
            app.UseCookiePolicy(
            new CookiePolicyOptions
            {
                Secure = CookieSecurePolicy.Always
            });
            app.UseSession();
            //---------------------------------------------
           // app.UseHttpsRedirection();
            //app.UseDeveloperExceptionPage();

            app.UseRouting();
            //----Added on 15-05-2024----
            app.UseCors();
            //---------------------------
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
