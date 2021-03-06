﻿// ***********************************************************************
// Assembly         : IronyModManager.Localization
// Author           : Mario
// Created          : 01-18-2020
//
// Last Modified By : Mario
// Last Modified On : 02-04-2020
// ***********************************************************************
// <copyright file="BaseLocalizationResourceProvider.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;
using System;
using System.IO;
using IronyModManager.Shared;

/// <summary>
/// The ResourceProviders namespace.
/// </summary>
namespace IronyModManager.Localization.ResourceProviders
{
    /// <summary>
    /// Class BaseLocalizationResourceProvider.
    /// Implements the <see cref="IronyModManager.Localization.ILocalizationResourceProvider" />
    /// Implements the <see cref="IronyModManager.Localization.ResourceProviders.ILocalizationResourceProvider" />
    /// </summary>
    /// <seealso cref="IronyModManager.Localization.ResourceProviders.ILocalizationResourceProvider" />
    /// <seealso cref="IronyModManager.Localization.ILocalizationResourceProvider" />
    [ExcludeFromCoverage("IO operations should not be tested.")]
    public abstract class BaseLocalizationResourceProvider : ILocalizationResourceProvider
    {
        #region Properties

        /// <summary>
        /// Gets the root path.
        /// </summary>
        /// <value>The root path.</value>
        public abstract string RootPath { get; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Reads the resource.
        /// </summary>
        /// <param name="locale">The locale.</param>
        /// <returns>System.String.</returns>
        public virtual string ReadResource(string locale)
        {
            var path = Path.Combine(RootPath, $"{locale}{Shared.Constants.JsonExtension}");
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            return string.Empty;
        }

        #endregion Methods
    }
}
