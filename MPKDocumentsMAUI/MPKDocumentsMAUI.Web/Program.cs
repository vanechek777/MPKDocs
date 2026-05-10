using MPKDocumentsMAUI.Shared.Api;
using MPKDocumentsMAUI.Shared.Services;
using MPKDocumentsMAUI.Web.Components;
using MPKDocumentsMAUI.Web.Services;

namespace MPKDocumentsMAUI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Add device-specific services used by the MPKDocumentsMAUI.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            builder.Services.AddSingleton<IMobileShellService, MobileShellService>();
            builder.Services.AddSingleton<IQrScanService, NullQrScanService>();
            builder.Services.AddSingleton<IDocumentFilePicker, NullDocumentFilePicker>();
            builder.Services.AddSingleton(new ApiOptions());
            builder.Services.AddSingleton<INotificationPermissionService, WebNotificationPermissionService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddAdditionalAssemblies(typeof(MPKDocumentsMAUI.Shared._Imports).Assembly);

            app.Run();
        }
    }
}
