using System.ComponentModel;
using TrailBuddy.ViewModels;
namespace TrailBuddy.Views;

public partial class FavoritesPage : ContentPage
{
    FavoritesViewModel _viewModel;
	public FavoritesPage()
	{
		InitializeComponent();

        _viewModel = new FavoritesViewModel(Navigation);
        BindingContext = _viewModel;
    }

    void ItemsListView_ItemTapped(System.Object sender, Microsoft.Maui.Controls.ItemTappedEventArgs e)
    {
        Favorites f = e.Item as Favorites;
        Navigation.PushAsync(new InfoPage(null, f));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Refreshes list data upon loading
        _viewModel.PopulatePage();
    }
}
