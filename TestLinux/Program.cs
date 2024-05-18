using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TestLinux
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    
                   webBuilder.UseStartup<Startup>();
                    webBuilder.UseKestrel( options =>
                    {
                        //----Added on 09-06-2022(to remove the server HTTP header details)-------
                        options.AddServerHeader = false;
                        //-----------------------------------------
                    });
                    //webBuilder.UseIIS();
                     //webBuilder.UseUrls("http://cgbank.in:5000");
                     webBuilder.UseUrls("http://cgbank.in:8080");
                    //string webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
                    //webBuilder.UseWebRoot(webRoot);

                    //----Added on 09-06-2022(to enforce the TLS 1.2 over TLS 1.0/1.1)-------
                    //webBuilder.ConfigureKestrel(options =>
                    //{
                    //    options.ConfigureHttpsDefaults(configureOptions =>
                    //    {
                    //        configureOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                    //    });
                    //}).UseStartup<Startup>();
                    //-----------------------------------------
                });
    }
}
