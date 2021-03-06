﻿// ***********************************************************************
// Assembly         : IronyModManager.Services.Common
// Author           : Mario
// Created          : 01-11-2020
//
// Last Modified By : Mario
// Last Modified On : 04-19-2020
// ***********************************************************************
// <copyright file="IPreferencesService.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************

using IronyModManager.Models.Common;

/// <summary>
/// The Common namespace.
/// </summary>
namespace IronyModManager.Services.Common
{
    /// <summary>
    /// Interface IPreferencesService
    /// Implements the <see cref="IronyModManager.Services.Common.IBaseService" />
    /// </summary>
    /// <seealso cref="IronyModManager.Services.Common.IBaseService" />
    public interface IPreferencesService : IBaseService
    {
        #region Methods

        /// <summary>
        /// Gets this instance.
        /// </summary>
        /// <returns>IPreferences.</returns>
        IPreferences Get();

        /// <summary>
        /// Saves the specified preferences.
        /// </summary>
        /// <param name="preferences">The preferences.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool Save(IPreferences preferences);

        #endregion Methods
    }
}
