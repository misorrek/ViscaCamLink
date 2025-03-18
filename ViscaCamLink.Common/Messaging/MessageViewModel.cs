namespace ViscaCamLink.Common.Messaging;

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

public class MessageViewModel : BaseViewModel
{
    public MessageViewModel()
    {
        _title = string.Empty;
        _text = string.Empty;
        _icon = Geometry.Empty;
        _okButtonText = string.Empty;
        _cancelButtonText = string.Empty;

        OkCommand = new Command(ExecuteOk);
        CancelCommand = new Command(ExecuteCancel);
    }

    private const string IconGeometryInformation = "M 13.5,6.5 A 1.5,1.5 0 0 1 12,8 1.5,1.5 0 0 1 10.5,6.5 1.5,1.5 0 0 1 12,5 1.5,1.5 0 0 1 13.5,6.5 Z M 12,10 h -1 a 1,1 0 0 0 0,2 h 1 v 6 a 1,1 0 0 0 2,0 v -6 a 2,2 0 0 0 -2,-2 z";
    private const string IconGeometryQuestion = "M 12,16 c 0.828,0 1.5,0.672 1.5,1.5 0,0.828 -0.672,1.5 -1.5,1.5 -0.828,0 -1.5,-0.672 -1.5,-1.5 0,-0.828 0.672,-1.5 1.5,-1.5 z m 1,-2 c 0,-0.561 0.408,-1.225 0.928,-1.512 1.5,-0.827 2.307,-2.523 2.009,-4.222 C 15.654,6.652 14.329,5.328 12.716,5.046 11.538,4.841 10.337,5.156 9.429,5.919 8.521,6.682 8,7.798 8,8.983 c 0,0.552 0.448,1 1,1 0.552,0 1,-0.448 1,-1 0,-0.592 0.261,-1.151 0.715,-1.533 0.461,-0.388 1.049,-0.542 1.656,-0.435 0.787,0.138 1.458,0.809 1.596,1.596 0.153,0.87 -0.241,1.704 -1.004,2.125 C 11.807,11.373 11,12.715 11,14 c 0,0.553 0.448,1 1,1 0.552,0 1,-0.447 1,-1 z";
    private const string IconGeometryWarning = "M 12 4.9999695 A 1 1 0 0 0 10.999969 6 L 10.999969 13.999969 A 1.0000305 1.0000305 0 0 0 13.000031 13.999969 L 13.000031 6 A 1 1 0 0 0 12 4.9999695 z M 12 16.999969 C 11.446001 16.999969 10.999969 17.446001 10.999969 18 C 10.999969 18.553999 11.446001 19.000031 12 19.000031 C 12.553999 19.000031 13.000031 18.553999 13.000031 18 C 13.000031 17.446001 12.553999 16.999969 12 16.999969 z";
    private const string IconGeometryError = "M 23.707 0.293 h 0 a 1 1 0 0 0 -1.414 0 L 12 10.586 L 1.707 0.293 a 1 1 0 0 0 -1.414 0 h 0 a 1 1 0 0 0 0 1.414 L 10.586 12 L 0.293 22.293 a 1 1 0 0 0 0 1.414 h 0 a 1 1 0 0 0 1.414 0 L 12 13.414 L 22.293 23.707 a 1 1 0 0 0 1.414 0 h 0 a 1 1 0 0 0 0 -1.414 L 13.414 12 L 23.707 1.707 A 1 1 0 0 0 23.707 0.293 Z";

    private static readonly Color IconBackgroundColorInformation = Color.FromRgb(40, 140, 245);
    private static readonly Color IconBackgroundColorQuestion = Color.FromRgb(40, 140, 245);
    private static readonly Color IconBackgroundColorWarning = Color.FromRgb(255, 170, 70);
    private static readonly Color IconBackgroundColorError = Color.FromRgb(255, 70, 70);

    #region Commands

    public ICommand OkCommand { get; set; }

    public ICommand CancelCommand { get; set; }

    #endregion

    #region Bindable properties

    public string Title
    {
        get => _title;

        set
        {
            _title = value;
            NotifyPropertyChanged();
        }
    }

    private string _title;

    public string Text
    {
        get => _text;

        set
        {
            _text = value;
            NotifyPropertyChanged();
        }
    }

    private string _text;

    public Geometry Icon
    {
        get => _icon;

        private set
        {
            _icon = value;
            NotifyPropertyChanged();
        }
    }

    private Geometry _icon;

    public Color IconBackgroundColor
    {
        get => _iconBackgroundColor;

        private set
        {
            _iconBackgroundColor = value;
            NotifyPropertyChanged();
        }
    }

    private Color _iconBackgroundColor;

    public bool OkButtonVisible
    {
        get => _okButtonVisible;

        private set
        {
            _okButtonVisible = value;
            NotifyPropertyChanged();
        }
    }

    private bool _okButtonVisible;

    public string OkButtonText
    {
        get => _okButtonText;

        set
        {
            _okButtonText = value;
            NotifyPropertyChanged();
        }
    }

    private string _okButtonText;

    public bool CancelButtonVisible
    {
        get => _cancelButtonVisible;

        private set
        {
            _cancelButtonVisible = value;
            NotifyPropertyChanged();
        }
    }

    private bool _cancelButtonVisible;

    public string CancelButtonText
    {
        get => _cancelButtonText;

        set
        {
            _cancelButtonText = value;
            NotifyPropertyChanged();
        }
    }

    private string _cancelButtonText;

    #endregion

    public MessageBoxImage Image
    {
        get => _image;

        set
        {
            _image = value;
            UpdateImageProperties();
        }
    }

    private MessageBoxImage _image;

    public MessageBoxButton Button
    {
        get => _button;

        set
        {
            _button = value;
            UpdateButtonProperties();
        }
    }

    private MessageBoxButton _button;

    public MessageBoxResult Result { get; private set; }

    private void UpdateImageProperties()
    {
        switch (Image)
        {
            case MessageBoxImage.None:
                Icon = Geometry.Empty;
                IconBackgroundColor = Color.FromArgb(0, 0, 0, 0);
                break;
            case MessageBoxImage.Information:
                Icon = Geometry.Parse(IconGeometryInformation);
                IconBackgroundColor = IconBackgroundColorInformation;
                break;
            case MessageBoxImage.Question:
                Icon = Geometry.Parse(IconGeometryQuestion);
                IconBackgroundColor = IconBackgroundColorQuestion;
                break;
            case MessageBoxImage.Warning:
                Icon = Geometry.Parse(IconGeometryWarning);
                IconBackgroundColor = IconBackgroundColorWarning;
                break;
            case MessageBoxImage.Error:
                Icon = Geometry.Parse(IconGeometryError);
                IconBackgroundColor = IconBackgroundColorError;
                break;
            default:
                throw new NotImplementedException($"Handling for image type \"{Image}\" is not implemented");
        }
    }

    private void UpdateButtonProperties()
    {
        switch (Button)
        {
            case MessageBoxButton.OK:
                OkButtonVisible = true;
                CancelButtonVisible = false;
                break;
            case MessageBoxButton.OKCancel:
            case MessageBoxButton.YesNo:
                OkButtonVisible = true;
                CancelButtonVisible = true;
                break;
            default:
                throw new NotImplementedException($"Handling for button type \"{Button}\" is not implemented");
        }
    }

    private void ExecuteOk()
    {
        Result = MessageBoxResult.OK;
        RequestCloseDialog?.Invoke();
    }

    private void ExecuteCancel()
    {
        Result = MessageBoxResult.Cancel;
        RequestCloseDialog?.Invoke();
    }
}
