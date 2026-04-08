// <copyright file="MemoryViewModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Events_GSS.ViewModels
{
    using Events_GSS.Data.Services.Interfaces;
    using Events_GSS.Data.ViewModels;

    /// <summary>
    /// The WinUI wrapper for the memory view model. 
    /// Inherits all logic from the testable Core in the Data project.
    /// </summary>
    public class MemoryViewModel : MemoryViewModelCore
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryViewModel"/> class.
        /// </summary>
        /// <param name="memoryService">The memory service.</param>
        public MemoryViewModel(IMemoryService memoryService)
            : base(memoryService)
        {
        }
    }
}