﻿
// ***********************************************************************
// Assembly         : IronyModManager
// Author           : Mario
// Created          : 06-14-2020
//
// Last Modified By : Mario
// Last Modified On : 06-24-2023
// ***********************************************************************
// <copyright file="ConflictSolverDBSearchControlViewModel.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using IronyModManager.Common;
using IronyModManager.Common.ViewModels;
using IronyModManager.Localization.Attributes;
using IronyModManager.Models.Common;
using IronyModManager.Shared;
using ReactiveUI;

namespace IronyModManager.ViewModels.Controls
{

    /// <summary>
    /// Class ConflictSolverDBSearchControlViewModel.
    /// Implements the <see cref="IronyModManager.Common.ViewModels.BaseViewModel" />
    /// </summary>
    /// <seealso cref="IronyModManager.Common.ViewModels.BaseViewModel" />
    [ExcludeFromCoverage("This should be tested via functional testing.")]
    public class ConflictSolverDBSearchControlViewModel : BaseViewModel
    {

        #region Properties

        /// <summary>
        /// Gets or sets the clear.
        /// </summary>
        /// <value>The clear.</value>
        [StaticLocalization(LocalizationResources.Conflict_Solver.DBSearch.Clear)]
        public virtual string Clear { get; protected set; }

        /// <summary>
        /// Gets or sets the clear command.
        /// </summary>
        /// <value>The clear command.</value>
        public virtual ReactiveCommand<Unit, Unit> ClearCommand { get; protected set; }

        /// <summary>
        /// Gets or sets the close.
        /// </summary>
        /// <value>The close.</value>
        [StaticLocalization(LocalizationResources.Conflict_Solver.DBSearch.Close)]
        public virtual string Close { get; protected set; }

        /// <summary>
        /// Gets or sets the close command.
        /// </summary>
        /// <value>The close command.</value>
        public virtual ReactiveCommand<Unit, Unit> CloseCommand { get; protected set; }

        /// <summary>
        /// Gets or sets the conflict result.
        /// </summary>
        /// <value>The conflict result.</value>
        public virtual IConflictResult ConflictResult { get; protected set; }

        /// <summary>
        /// Gets or sets the database search.
        /// </summary>
        /// <value>The database search.</value>
        [StaticLocalization(LocalizationResources.Conflict_Solver.DBSearch.Title)]
        public virtual string DatabaseSearch { get; protected set; }

        /// <summary>
        /// Gets or sets the database search command.
        /// </summary>
        /// <value>The database search command.</value>
        public virtual ReactiveCommand<Unit, Unit> DatabaseSearchCommand { get; protected set; }

        /// <summary>
        /// Gets or sets the definitions.
        /// </summary>
        /// <value>The definitions.</value>
        public virtual IEnumerable<string> Definitions { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is open.
        /// </summary>
        /// <value><c>true</c> if this instance is open; otherwise, <c>false</c>.</value>
        public virtual bool IsOpen { get; protected set; }

        /// <summary>
        /// Gets or sets the search term.
        /// </summary>
        /// <value>The search term.</value>
        public virtual string SearchTerm { get; protected set; }

        /// <summary>
        /// Gets or sets the watermark.
        /// </summary>
        /// <value>The watermark.</value>
        [StaticLocalization(LocalizationResources.Conflict_Solver.DBSearch.Watermark)]
        public virtual string Watermark { get; protected set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Forces the close popup.
        /// </summary>
        public virtual void ForceClosePopup()
        {
            IsOpen = false;
        }

        /// <summary>
        /// Sets the parameters.
        /// </summary>
        /// <param name="conflictResult">The conflict result.</param>
        public void SetParameters(IConflictResult conflictResult)
        {
            ConflictResult = conflictResult;
        }

        /// <summary>
        /// Called when [activated].
        /// </summary>
        /// <param name="disposables">The disposables.</param>
        protected override void OnActivated(CompositeDisposable disposables)
        {
            CloseCommand = ReactiveCommand.Create(() =>
            {
                ForceClosePopup();
            }).DisposeWith(disposables);

            DatabaseSearchCommand = ReactiveCommand.Create(() =>
            {
                IsOpen = true;
            }).DisposeWith(disposables);

            ClearCommand = ReactiveCommand.Create(() =>
            {
                SearchTerm = string.Empty;
                Definitions = null;
            }).DisposeWith(disposables);

            this.WhenAnyValue(p => p.SearchTerm).Where(s => !string.IsNullOrWhiteSpace(s) && s.Length >= 2).Subscribe(async s =>
            {
                if (ConflictResult != null && ConflictResult.AllConflicts != null)
                {
                    var result = await ConflictResult.AllConflicts.SearchDefinitionsAsync(s);
                    if (result != null)
                    {
                        Definitions = result.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToObservableCollection();
                    }
                    else
                    {
                        Definitions = null;
                    }
                }
            }).DisposeWith(disposables);

            base.OnActivated(disposables);
        }

        #endregion Methods
    }
}
