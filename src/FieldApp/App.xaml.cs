using FieldApp.Sync;

namespace FieldApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new ContentPage { Content = new Label { Text = "Field App" } };
    }
}
