﻿// ***********************************************************************
// Assembly         : IronyModManager.Storage
// Author           : Mario
// Created          : 01-11-2020
//
// Last Modified By : Mario
// Last Modified On : 03-03-2020
// ***********************************************************************
// <copyright file="Storage.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using IronyModManager.DI;
using IronyModManager.Models.Common;
using IronyModManager.Storage.Common;

namespace IronyModManager.Storage
{
    /// <summary>
    /// Class Storage.
    /// Implements the <see cref="IronyModManager.Storage.Common.IStorageProvider" />
    /// </summary>
    /// <seealso cref="IronyModManager.Storage.Common.IStorageProvider" />
    public class Storage : IStorageProvider
    {
        #region Fields

        /// <summary>
        /// The database lock
        /// </summary>
        private static readonly object dbLock = new { };

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Storage" /> class.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="mapper">The mapper.</param>
        public Storage(IDatabase database, IMapper mapper)
        {
            Database = database;
            Mapper = mapper;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <value>The database.</value>
        protected IDatabase Database { get; private set; }

        /// <summary>
        /// Gets the mapper.
        /// </summary>
        /// <value>The mapper.</value>
        protected IMapper Mapper { get; private set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Gets the state of the application.
        /// </summary>
        /// <returns>IAppState.</returns>
        public IAppState GetAppState()
        {
            lock (dbLock)
            {
                return Database.AppState;
            }
        }

        /// <summary>
        /// Gets the games.
        /// </summary>
        /// <returns>IEnumerable&lt;IGameType&gt;.</returns>
        public virtual IEnumerable<IGameType> GetGames()
        {
            lock (dbLock)
            {
                return Database.Games;
            }
        }

        /// <summary>
        /// Gets the preferences.
        /// </summary>
        /// <returns>IPreferences.</returns>
        public virtual IPreferences GetPreferences()
        {
            lock (dbLock)
            {
                var result = Mapper.Map<IPreferences, IPreferences>(Database.Preferences);
                return result;
            }
        }

        /// <summary>
        /// Gets the themes.
        /// </summary>
        /// <returns>Dictionary&lt;System.String, IEnumerable&lt;System.String&gt;&gt;.</returns>
        public virtual IEnumerable<IThemeType> GetThemes()
        {
            lock (dbLock)
            {
                return Database.Themes;
            }
        }

        /// <summary>
        /// Gets the state of the window.
        /// </summary>
        /// <returns>IWindowState.</returns>
        public virtual IWindowState GetWindowState()
        {
            lock (dbLock)
            {
                var result = Mapper.Map<IWindowState, IWindowState>(Database.WindowState);
                return result;
            }
        }

        /// <summary>
        /// Registers the game.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="appId">The application identifier.</param>
        /// <param name="userDirectory">The user directory.</param>
        /// <param name="workshopDirectory">The workshop directory.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public virtual bool RegisterGame(string name, int appId, string userDirectory, string workshopDirectory)
        {
            lock (dbLock)
            {
                if (Database.Games.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"{name} game is already registered.");
                }
                var game = DIResolver.Get<IGameType>();
                game.Name = name;
                game.UserDirectory = userDirectory;
                game.SteamAppId = appId;
                game.WorkshopDirectory = workshopDirectory;
                Database.Games.Add(game);
                return true;
            }
        }

        /// <summary>
        /// Registers the theme.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="styles">The styles.</param>
        /// <param name="brushes">The brushes.</param>
        /// <param name="isDefault">if set to <c>true</c> [is default].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="InvalidOperationException">There is already a default theme registered.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="InvalidOperationException">There is already a default theme registered.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        public virtual bool RegisterTheme(string name, IEnumerable<string> styles, IDictionary<string, string> brushes, bool isDefault = false)
        {
            lock (dbLock)
            {
                if (isDefault && Database.Themes.Any(s => s.IsDefault))
                {
                    throw new InvalidOperationException("There is already a default theme registered.");
                }
                if (Database.Themes.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"{name} theme is already registered.");
                }
                var themeType = DIResolver.Get<IThemeType>();
                themeType.IsDefault = isDefault;
                themeType.Name = name;
                themeType.Styles = styles ?? new List<string>();
                themeType.Brushes = brushes ?? new Dictionary<string, string>();
                Database.Themes.Add(themeType);
                return true;
            }
        }

        /// <summary>
        /// Sets the state of the application.
        /// </summary>
        /// <param name="appState">State of the application.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool SetAppState(IAppState appState)
        {
            lock (dbLock)
            {
                Database.AppState = appState;
                return true;
            }
        }

        /// <summary>
        /// Sets the preferences.
        /// </summary>
        /// <param name="preferences">The preferences.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public virtual bool SetPreferences(IPreferences preferences)
        {
            lock (dbLock)
            {
                Database.Preferences = preferences;
                return true;
            }
        }

        /// <summary>
        /// Sets the state of the window.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public virtual bool SetWindowState(IWindowState state)
        {
            lock (dbLock)
            {
                Database.WindowState = state;
                return true;
            }
        }

        #endregion Methods
    }
}
