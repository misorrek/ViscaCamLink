namespace ViscaCamLink.Factories;

using ViscaCamLink.ViewModels;
using ViscaCamLink.Views;

public static class OptionsViewFactory
{
    public static OptionsView CreateOptionsView()
    {
        var view = new OptionsView();
        var viewModel = new OptionsViewModel(() => view.Close());

        view.DataContext = viewModel;

        return view;
    }
}
