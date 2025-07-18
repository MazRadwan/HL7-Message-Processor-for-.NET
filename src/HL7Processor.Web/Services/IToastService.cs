namespace HL7Processor.Web.Services;

public interface IToastService
{
    event Action<ToastMessage>? OnShow;
    void ShowSuccess(string message, string title = "Success");
    void ShowError(string message, string title = "Error");
    void ShowWarning(string message, string title = "Warning");
    void ShowInfo(string message, string title = "Information");
}

public class ToastMessage
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int AutoHideDelay { get; set; } = 5000;
}