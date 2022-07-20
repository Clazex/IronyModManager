﻿// ***********************************************************************
// Assembly         : IronyModManager.Parser.Common.Args
// Author           : Mario
// Created          : 02-19-2020
//
// Last Modified By : Mario
// Last Modified On : 07-20-2022
// ***********************************************************************
// <copyright file="ParserManagerArgs.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;

namespace IronyModManager.Parser.Common.Args
{
    /// <summary>
    /// Class ParserManagerArgs.
    /// </summary>
    public class ParserManagerArgs
    {
        #region Properties

        /// <summary>
        /// Gets or sets the content sha.
        /// </summary>
        /// <value>The content sha.</value>
        public string ContentSHA { get; set; }

        /// <summary>
        /// Gets or sets the file.
        /// </summary>
        /// <value>The file.</value>
        public string File { get; set; }

        /// <summary>
        /// Gets or sets the file last modified.
        /// </summary>
        /// <value>The file last modified.</value>
        public DateTime? FileLastModified { get; set; }

        /// <summary>
        /// Gets or sets the type of the game.
        /// </summary>
        /// <value>The type of the game.</value>
        public string GameType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is binary.
        /// </summary>
        /// <value><c>true</c> if this instance is binary; otherwise, <c>false</c>.</value>
        public bool IsBinary { get; set; }

        /// <summary>
        /// Gets or sets the lines.
        /// </summary>
        /// <value>The lines.</value>
        public IEnumerable<string> Lines { get; set; }

        /// <summary>
        /// Gets or sets the mod dependencies.
        /// </summary>
        /// <value>The mod dependencies.</value>
        public IEnumerable<string> ModDependencies { get; set; }

        /// <summary>
        /// Gets or sets the name of the mod.
        /// </summary>
        /// <value>The name of the mod.</value>
        public string ModName { get; set; }

        /// <summary>
        /// Gets or sets the type of the validation.
        /// </summary>
        /// <value>The type of the validation.</value>
        public ValidationType ValidationType { get; set; }

        #endregion Properties
    }
}
