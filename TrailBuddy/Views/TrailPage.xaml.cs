using Microsoft.Maui.Storage;
using TrailBuddy.ViewModels;

namespace TrailBuddy.Views;

public partial class TrailPage : ContentPage
{
    private TrailViewModel _viewModel;
    private int startingNum = 0;
    private int navNum = 0;
    private string pageTitle;

	public TrailPage(TrailData data, string title)
	{
		InitializeComponent();
        pageTitle = title;
        _viewModel = new TrailViewModel(data, Navigation, title);
        BindingContext = _viewModel;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (startingNum == 0)
        {
            filterPicker.SelectedIndex = 1;
            startingNum++;
        }

        if (pageTitle.Equals("Nearby Search")) filterPicker.SelectedIndex = 3;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        if (navNum > 0)
        {
            _viewModel.RefreshFromFilters();
        }

        navNum++;

    }

    void ItemsListView_ItemSelected(System.Object sender, Microsoft.Maui.Controls.SelectedItemChangedEventArgs e)
    {
        Place p = e.SelectedItem as Place;

        // Do nothing if its a placeholder
        if (p.Name != "Loading ...")
            Navigation.PushAsync(new InfoPage(p, null));
    }

    void ItemsListView_Scrolled(System.Object sender, Microsoft.Maui.Controls.ScrolledEventArgs e)
    {
        // If you reach the end of the scroll, it'll add more to the list view.
        int itemCount = ItemsListView.ItemsSource.Cast<object>().Count();
        int targetPos = itemCount * 170 - 1750;

        // If past pos and nothing is currently loading
        if (e.ScrollY >= targetPos && Preferences.Get("loadMore", false))
        {
            LoadMore();
        }
    }

    private async Task LoadMore()
    {
        await _viewModel.LoadMore();
        ItemsListView.ItemsSource = _viewModel.Places;
    }
}
