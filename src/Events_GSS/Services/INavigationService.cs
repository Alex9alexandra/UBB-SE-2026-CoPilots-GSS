namespace Events_GSS.Services;

public interface INavigationService
{
    /// <summary>
    /// Navigate to a page by its registered key.
    /// </summary>
    void NavigateTo(string pageKey);

    /// <summary>
    /// Navigate to a page passing a parameter (e.g. an Event object).
    /// </summary>
    void NavigateTo(string pageKey, object parameter);

    /// <summary>
    /// Go back to the previous page.
    /// </summary>
    void GoBack();

    /// <summary>
    /// Whether there is a page to go back to.
    /// </summary>
    bool CanGoBack { get; }
}
