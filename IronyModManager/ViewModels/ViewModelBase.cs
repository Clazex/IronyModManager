﻿// ***********************************************************************
// Assembly         : IronyModManager
// Author           : Mario
// Created          : 01-10-2020
//
// Last Modified By : Mario
// Last Modified On : 01-11-2020
// ***********************************************************************
// <copyright file="ViewModelBase.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;
using System;
using ReactiveUI;

namespace IronyModManager.ViewModels
{
    /// <summary>
    /// Class ViewModelBase.
    /// Implements the <see cref="ReactiveUI.ReactiveObject" />
    /// Implements the <see cref="IronyModManager.ViewModels.IViewModel" />
    /// </summary>
    /// <seealso cref="ReactiveUI.ReactiveObject" />
    /// <seealso cref="IronyModManager.ViewModels.IViewModel" />
    public class ViewModelBase : ReactiveObject, IViewModel
    {
    }
}
