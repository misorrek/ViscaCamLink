namespace ViscaCamLink.Factories
{
    using AutoUpdaterDotNET;

    using ViscaCamLink.ViewModels;
    using ViscaCamLink.Views;

    public static class UpdateViewFactory
    {
        public static UpdateView CreateUpdateView(UpdateInfoEventArgs updateInfoEventArgs)
        {
            var view = new UpdateView();
            var viewModel = new UpdateViewModel(updateInfoEventArgs, () => view.Close());
            
            view.DataContext = viewModel;

            return view;
        }
    }
}
