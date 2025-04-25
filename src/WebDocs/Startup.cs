using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;
using Sysphera.Middleware.Drapo;
using Microsoft.Net.Http.Headers;
using System.IO;
using WebDocs.Controllers;
using System.Text.RegularExpressions;
using WebDocs.Models;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Hosting;
using Sysphera.Middleware.Drapo.Pipe;

namespace WebDocs
{
    public class Startup
    {
        DrapoMiddlewareOptions _options = null;
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddSignalR().AddJsonProtocol(options => options.PayloadSerializerOptions.PropertyNamingPolicy = null);
            services.AddDrapo();
            services.AddControllersWithViews()
                  .AddNewtonsoftJson(options =>
                  {
                      options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                  })
                  .AddJsonOptions(options =>
                  {
                      options.JsonSerializerOptions.PropertyNamingPolicy = null;
                  });
            services.AddSingleton<MenuController, MenuController>();
            services.AddSingleton<FunctionController, FunctionController>();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Drapo Docs API",
                    Version = "v1",
                    Description = "API to be used in the drapo docs",
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, MenuController menu)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseDrapo(o => { ConfigureDrapo(env, o, menu); });
            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions()
            {
                OnPrepareResponse = (context) =>
                {
                    var headers = context.Context.Response.GetTypedHeaders();
                    headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                }
            });
            app.UseRouting();
            app.UseAuthentication();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<DrapoPlumberHub>(string.Format("/{0}", _options.Config.PipeHubName));
            });
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebSPA API");
            });
        }

        private void ConfigureDrapo(IWebHostEnvironment env, DrapoMiddlewareOptions options, MenuController menu)
        {
            if (env.IsDevelopment())
                options.Debug = true;
            options.Config.UseRouter = false;
            options.Config.UsePipes = true;
            options.Config.CreateTheme("", "");
            options.Config.CreateTheme("Dark", "dark");
            options.Config.CreateView("Mobile", "mobile", "{{__browser.Width}} < 768");
            options.Config.CreateView("Web", "default");
            options.Config.StorageErrors = "errors";
            options.Config.ValidatorUncheckedClass = "ppValidationUnchecked";
            options.Config.ValidatorValidClass = "ppValidatorValid";
            options.Config.ValidatorInvalidClass = "ppValidatorInvalid";
            options.Config.OnError = "UncheckItemField({{dkLayoutMenuState.menu}});ClearItemField({{taError.Container}});ClearSector(rainbow);ClearSector(footer);UpdateSector(content,/app/error/index.html,Error,true,true,{{tabError.Container}});UncheckDataField(dkTabs,Selected,false);AddDataItem(dkTabs,{{tabError}})";
            options.Config.LoadComponents(string.Format("{0}{1}components", env.WebRootPath, Path.AltDirectorySeparatorChar), "~/components");
            options.Config.HandlerCustom = h => HandlerCustom(h, menu);
            options.PollingEvent += Polling;
            options.Config.CreateRoute("^/function/(?<id>\\w+)$", "ClearSector(content);UpdateSector(content,~/app/shared/function.html)");
            options.Config.CreateRoute("^/doc/(?<id>\\w+)$", "ClearSector(content);UpdateSector(content,~/app/shared/doc.html)");
            options.Config.CreateRoute("^/$", "UpdateSector(content,~/app/menu/0000%20-%20Drapo.html)");
            this._options = options;
        }

        private async Task<DrapoDynamic> HandlerCustom(DrapoDynamic dynamic, MenuController menu)
        {
            List<MenuItemVM> items = await menu.GetItemsInternal();
            foreach (Match match in Regex.Matches(dynamic.ContentData, @"\[(?<label>(\w|\s|\-)+)\]\((?<item>(\w|\s|\-)+)\)"))
            {
                string label = match.Groups["label"].Value;
                string item = match.Groups["item"].Value;
                MenuItemVM menuItem = this.GetItem(items, item);
                if (menuItem == null)
                    continue;
                string content = $"<span class='dContentLink' d-on-click='{menuItem.Action}'>{label}</span>";
                dynamic.ContentData = dynamic.ContentData.Replace(match.Value, content);
            }
            return (await Task.FromResult<DrapoDynamic>(dynamic));
        }

        private MenuItemVM GetItem(List<MenuItemVM> items, string name)
        {
            foreach (MenuItemVM item in items)
            {
                if (item.Name == name)
                    return (item);
                MenuItemVM itemChild = GetItem(item.Items, name);
                if (itemChild != null)
                    return (itemChild);
            }
            return (null);
        }

        private async Task<string> Polling(string domain, string connectionId, string key)
        {
            string hash = Math.Ceiling((decimal)(DateTime.Now.Second / 5)).ToString();
            return (await Task.FromResult<string>(hash));
        }
    }
}
