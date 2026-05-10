using Microsoft.Maui.Controls;

#if WINDOWS
using System.Reflection;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
#endif

namespace MPKDocumentsMAUI
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
#if WINDOWS
            blazorWebView.BlazorWebViewInitializing += OnBlazorWebViewInitializing;
            blazorWebView.BlazorWebViewInitialized += OnBlazorWebViewInitialized;
#endif
        }

#if WINDOWS
        /// <summary>
        /// Пустой <c>EnvironmentOptions</c> на пути инициализации WebView2 даёт NRE в <c>ApplyDefaultWebViewSettings</c>.
        /// </summary>
        private static void OnBlazorWebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e) =>
            e.EnvironmentOptions ??= new CoreWebView2EnvironmentOptions();

        /// <summary>
        /// На части сборок WinUI у <c>BlazorWebView</c> есть свойство <c>Services</c> не в публичной поверхности —
        /// подставляем <see cref="IServiceProvider"/> через рефлексию, если свойство есть.
        /// </summary>
        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();
            TryAssignBlazorWebViewServicesIfPropertyExists();
        }

        private void TryAssignBlazorWebViewServicesIfPropertyExists()
        {
            try
            {
                var sp = Handler?.MauiContext?.Services
                    ?? Application.Current?.Handler?.MauiContext?.Services;
                if (sp is null)
                    return;

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var prop = blazorWebView.GetType().GetProperty("Services", flags);
                if (prop?.CanWrite != true || !typeof(IServiceProvider).IsAssignableFrom(prop.PropertyType))
                    return;
                if (prop.GetValue(blazorWebView) is not null)
                    return;
                prop.SetValue(blazorWebView, sp);
            }
            catch
            {
                // ignore
            }
        }

        private static void OnBlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
        {
            if (e.WebView is not WebView2 wv)
                return;
            // По возможности разрешаем и DOM-drop (двойной канал с GestureRecognizer).
            TrySetAllowExternalDropReflection(wv);
            wv.CoreWebView2Initialized += (_, _) => TrySetAllowExternalDropReflection(wv);
        }

        private static void TrySetAllowExternalDropReflection(WebView2 wv)
        {
            try
            {
                if (wv.CoreWebView2 is null)
                    return;
                var core = wv.CoreWebView2;
                object? controller = null;
                var coreType = core.GetType();
                foreach (var p in coreType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                {
                    if (p.Name.Equals("Controller", StringComparison.Ordinal))
                    {
                        controller = p.GetValue(core);
                        break;
                    }
                }

                if (controller is null)
                {
                    foreach (var p in wv.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                    {
                        if (!p.Name.Contains("Controller", StringComparison.Ordinal))
                            continue;
                        var c = p.GetValue(wv);
                        if (c is null) continue;
                        var allow = c.GetType().GetProperty("AllowExternalDrop", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        if (allow is not null)
                        {
                            controller = c;
                            break;
                        }
                    }
                }

                if (controller is null)
                    return;
                var dropProp = controller.GetType().GetProperty("AllowExternalDrop", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (dropProp is not null && dropProp.CanWrite)
                    dropProp.SetValue(controller, true);
            }
            catch
            {
                // ignored
            }
        }
#endif
    }
}
