namespace MPKDocumentsMAUI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var main = new MainPage();
            NavigationPage.SetHasNavigationBar(main, false);
            return new Window(new NavigationPage(main)) { Title = "MPKDocumentsMAUI" };
        }
    }
}
