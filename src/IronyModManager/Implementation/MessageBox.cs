﻿// ***********************************************************************
// Assembly         : IronyModManager
// Author           : Mario
// Created          : 01-22-2020
//
// Last Modified By : Mario
// Last Modified On : 11-24-2022
// ***********************************************************************
// <copyright file="MessageBox.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using IronyModManager.DI;
using IronyModManager.Implementation.Actions;
using IronyModManager.Platform.Configuration;
using IronyModManager.Platform.Fonts;
using IronyModManager.Services.Common;
using IronyModManager.Shared;
using MessageBox.Avalonia.BaseWindows.Base;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.ViewModels;

namespace IronyModManager.Implementation
{
    /// <summary>
    /// Class MessageBoxes.
    /// </summary>
    [ExcludeFromCoverage("Won't test external GUI component.")]
    public static class MessageBoxes
    {
        #region Methods

        /// <summary>
        /// Gets the fatal error window.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="header">The header.</param>
        /// <param name="message">The message.</param>
        /// <returns>MsgBox.BaseWindows.IMsBoxWindow&lt;System.String&gt;.</returns>
        public static Controls.Themes.CustomMessageBox GetFatalErrorWindow(string title, string header, string message)
        {
            var font = ResolveFont();
            var parameters = new MessageBoxCustomParams
            {
                CanResize = false,
                ShowInCenter = true,
                ContentTitle = title,
                ContentHeader = header,
                ContentMessage = message,
                Icon = Icon.Error,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FontFamily = font.GetFontFamily(),
                WindowIcon = StaticResources.GetAppIcon()
            };
            var window = new Controls.Themes.CustomMessageBox();
            var config = DIResolver.Get<IPlatformConfiguration>().GetOptions();
            if (!config.TitleBar.Native)
            {
                window.ExtendClientAreaToDecorationsHint = true;
                window.ExtendClientAreaTitleBarHeightHint = 30d;
                window.Padding = new Thickness(0, 30, 0, 0);
            }
            window.DataContext = new MsBoxCustomViewModel(new MsCustomParams(parameters), window);
            return window;
        }

        /// <summary>
        /// Gets the prompt window.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="header">The header.</param>
        /// <param name="message">The message.</param>
        /// <param name="icon">The icon.</param>
        /// <param name="promptType">Type of the prompt.</param>
        /// <returns>IMsBoxWindow&lt;ButtonResult&gt;.</returns>
        public static IMsBoxWindow<ButtonResult> GetPromptWindow(string title, string header, string message, Icon icon, PromptType promptType)
        {
            var buttonEnum = promptType switch
            {
                PromptType.ConfirmCancel => ButtonEnum.OkCancel,
                PromptType.OK => ButtonEnum.Ok,
                _ => ButtonEnum.YesNo,
            };
            var font = ResolveFont();
            var parameters = new MessageBoxStandardParams()
            {
                CanResize = false,
                ShowInCenter = true,
                ContentTitle = title,
                ContentHeader = header,
                ContentMessage = message,
                Icon = icon,
                ButtonDefinitions = buttonEnum,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FontFamily = font.GetFontFamily(),
                WindowIcon = StaticResources.GetAppIcon()
            };
            var window = new Controls.Themes.StandardMessageBox(buttonEnum);
            var config = DIResolver.Get<IPlatformConfiguration>().GetOptions();
            if (!config.TitleBar.Native)
            {
                window.ExtendClientAreaToDecorationsHint = true;
                window.ExtendClientAreaTitleBarHeightHint = 30d;
                window.Padding = new Thickness(0, 30, 0, 0);
            }
            window.DataContext = new MsBoxStandardViewModel(parameters, window);
            return new StandardMessageBox(window);
        }

        /// <summary>
        /// Resolves the font.
        /// </summary>
        /// <returns>IFontFamily.</returns>
        private static IFontFamily ResolveFont()
        {
            var langService = DIResolver.Get<ILanguagesService>();
            var language = langService.GetSelected();
            var fontResolver = DIResolver.Get<IFontFamilyManager>();
            var font = fontResolver.ResolveFontFamily(language.Font);
            return font;
        }

        #endregion Methods
    }
}
