using TrailBuddy.ViewModels;

namespace TrailBuddy.Views;

public partial class InfoPage : ContentPage
{
	public InfoPage(Place p = null, Favorites f = null)
	{
		InitializeComponent();
        BindingContext = new InfoViewModel(p, f);
    }
}
