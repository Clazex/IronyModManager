﻿// ***********************************************************************
// Assembly         : IronyModManager
// Author           : Mario
// Created          : 09-30-2020
//
// Last Modified By : Mario
// Last Modified On : 03-14-2021
// ***********************************************************************
// <copyright file="BaseFontFamily.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace IronyModManager.Platform.Fonts
{
    /// <summary>
    /// Class BaseFontFamily.
    /// Implements the <see cref="IronyModManager.Platform.Fonts.IFontFamily" />
    /// </summary>
    /// <seealso cref="IronyModManager.Platform.Fonts.IFontFamily" />
    public abstract class BaseFontFamily : IFontFamily
    {
        #region Fields

        /// <summary>
        /// The instance
        /// </summary>
        private FontFamily instance;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the resource URI.
        /// </summary>
        /// <value>The resource URI.</value>
        public virtual string ResourceUri => $"font://Fonts/{Folder}#{Name}";

        /// <summary>
        /// Gets the folder.
        /// </summary>
        /// <value>The folder.</value>
        protected abstract string Folder { get; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Gets the font family.
        /// </summary>
        /// <returns>FontFamily.</returns>
        public FontFamily GetFontFamily()
        {
            if (instance == null)
            {
                instance = new FontFamily(ResourceUri);
            }
            return instance;
        }

        #endregion Methods
    }
}
