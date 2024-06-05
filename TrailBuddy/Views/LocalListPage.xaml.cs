namespace TrailBuddy.Views;
using TrailBuddy.ViewModels;
using Microsoft.Maui.Controls.Maps;

public partial class LocalListPage : ContentPage
{
    LocalListViewModel _viewModel;
    Polyline polyline;
    private int startingNum = 0;

    public LocalListPage()
	{
		InitializeComponent();
        _viewModel = new LocalListViewModel(Navigation);
        BindingContext = _viewModel;
	}

    void ItemsListView_ItemTapped(System.Object sender, Microsoft.Maui.Controls.ItemTappedEventArgs e)
    {
        // Open modal page with the trail info.
        LocalTrails lt = e.Item as LocalTrails;
        Navigation.PushModalAsync(new ModalPage(lt, "Modify Trail"));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.PopulatePage();

        if (startingNum == 0)
        {
            FilterPicker.SelectedIndex = 0;
            startingNum++;
        }
    }

    void FilterPicker_SelectedIndexChanged(System.Object sender, System.EventArgs e)
    {
    }
}


