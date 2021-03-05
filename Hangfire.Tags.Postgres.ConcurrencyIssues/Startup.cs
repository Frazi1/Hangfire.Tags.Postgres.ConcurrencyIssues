using System;
using Hangfire.PostgreSql;
using Hangfire.Tags;
using Hangfire.Tags.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Hangfire.Console.Extensions.InitalizationIssueRepro
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Async(c => c.File("logs.log", LogEventLevel.Information, rollingInterval: RollingInterval.Day, buffered: true, flushToDiskInterval: TimeSpan.FromSeconds(5)), bufferSize: 1000)
                .CreateLogger();
            
            services.AddLogging(builder => builder
                .SetMinimumLevel(LogLevel.Trace).AddConsole()
                .AddSerilog(logger)
            );
            
            services.AddHangfire((provider, configuration) => configuration
                .UsePostgreSqlStorage(Configuration.GetConnectionString("HangfirePostgres"))
                .UseTagsWithPostgreSql(new TagsOptions(){TagsListStyle = TagsListStyle.Dropdown})
            );
            services.AddHangfireServer(options =>
            {
                options.Queues = new[] {"default", "worker-queue"};
                options.WorkerCount = 3;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHangfireDashboard();
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Hello World!"); }); });

            RecurringJob.AddOrUpdate<SimpleJob>(job => job.RunGeneratorJob(JobType.Type1), Cron.Minutely);
            RecurringJob.AddOrUpdate<SimpleJob>(job => job.RunGeneratorJob(JobType.Type2), Cron.Minutely);
            RecurringJob.AddOrUpdate<SimpleJob>(job => job.RunGeneratorJob(JobType.Type3), Cron.Minutely);
            RecurringJob.AddOrUpdate<SimpleJob>(job => job.RunGeneratorJob(JobType.Type4), Cron.Minutely);
            RecurringJob.AddOrUpdate<SimpleJob>(job => job.RunGeneratorJob(JobType.Type5), Cron.Minutely);
            RecurringJob.AddOrUpdate<SimpleJob>(job => job.RunGeneratorJob(JobType.Type6), Cron.Minutely);
        }
    }
}