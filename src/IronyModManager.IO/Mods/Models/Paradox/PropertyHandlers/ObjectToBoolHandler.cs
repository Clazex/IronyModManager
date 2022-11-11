﻿// ***********************************************************************
// Assembly         : IronyModManager.IO
// Author           : Mario
// Created          : 08-13-2020
//
// Last Modified By : Mario
// Last Modified On : 11-04-2022
// ***********************************************************************
// <copyright file="ObjectToBoolHandler.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using RepoDb.Interfaces;
using RepoDb.Options;

namespace IronyModManager.IO.Mods.Models.Paradox.PropertyHandlers
{
    /// <summary>
    /// Class ObjectToBoolHandler.
    /// Implements the <see cref="RepoDb.Interfaces.IPropertyHandler{System.Object, System.Boolean}" />
    /// </summary>
    /// <seealso cref="RepoDb.Interfaces.IPropertyHandler{System.Object, System.Boolean}" />
    public class ObjectToBoolHandler : IPropertyHandler<object, bool>
    {
        #region Methods

        /// <summary>
        /// Gets the specified input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="options">The options.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool Get(object input, PropertyHandlerGetOptions options)
        {
            // The ORM now is officially giving me headaches... it just can't resolve a proper type, and I cannot target it with a specific type either.
            if (input == null)
            {
                return false;
            }
            if (input is long || input is int || input is short)
            {
                return Convert.ToInt32(input) > 0;
            }
            else if (input is bool boolean)
            {
                return boolean;
            }
            else if (int.TryParse(input.ToString(), out var intResult))
            {
                return intResult > 0;
            }
            else if (bool.TryParse(input.ToString(), out var boolresult))
            {
                return boolresult;
            }
            return false;
        }

        /// <summary>
        /// Sets the specified input.
        /// </summary>
        /// <param name="input">if set to <c>true</c> [input].</param>
        /// <param name="options">The options.</param>
        /// <returns>System.Object.</returns>
        public object Set(bool input, PropertyHandlerSetOptions options)
        {
            return input ? 1 : 0;
        }

        #endregion Methods
    }
}
