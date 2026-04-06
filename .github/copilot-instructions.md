# Copilot Instructions

## Project Guidelines
- The project uses MVVM Toolkit for ViewModels with @ObservableProperty and @RelayCommand attributes. CreateEventViewModel should be initialized with dependency-injected services (IUserService, etc.) rather than default constructors. Uses .NET 10 WinApp SDK for Windows 11 UI.