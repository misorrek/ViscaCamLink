namespace ViscaCamLink.Common;

using System;
using System.Windows.Input;

public interface IBaseViewModel
{
    ICommand CloseWindowCommand { get; }

    Action? RequestCloseDialog { get; set; }
}
