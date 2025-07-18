namespace HL7Processor.Web.Services;

public class ToastService : IToastService
{
    public event Action<ToastMessage>? OnShow;

    public void ShowSuccess(string message, string title = "Success")
    {
        OnShow?.Invoke(new ToastMessage
        {
            Type = "success",
            Title = title,
            Message = message,
            AutoHideDelay = 4000
        });
    }

    public void ShowError(string message, string title = "Error")
    {
        OnShow?.Invoke(new ToastMessage
        {
            Type = "danger",
            Title = title,
            Message = message,
            AutoHideDelay = 6000 // Errors stay longer
        });
    }

    public void ShowWarning(string message, string title = "Warning")
    {
        OnShow?.Invoke(new ToastMessage
        {
            Type = "warning",
            Title = title,
            Message = message,
            AutoHideDelay = 5000
        });
    }

    public void ShowInfo(string message, string title = "Information")
    {
        OnShow?.Invoke(new ToastMessage
        {
            Type = "info",
            Title = title,
            Message = message,
            AutoHideDelay = 4000
        });
    }
}