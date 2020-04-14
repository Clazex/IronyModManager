﻿// ***********************************************************************
// Assembly         : IronyModManager.Models
// Author           : Mario
// Created          : 02-29-2020
//
// Last Modified By : Mario
// Last Modified On : 04-10-2020
// ***********************************************************************
// <copyright file="Mod.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using IronyModManager.Models.Common;
using IronyModManager.Shared;

namespace IronyModManager.Models
{
    /// <summary>
    /// Class Mod.
    /// Implements the <see cref="IronyModManager.Models.Common.BaseModel" />
    /// Implements the <see cref="IronyModManager.Models.Common.IMod" />
    /// </summary>
    /// <seealso cref="IronyModManager.Models.Common.BaseModel" />
    /// <seealso cref="IronyModManager.Models.Common.IMod" />
    public class Mod : BaseModel, IMod
    {
        #region Properties

        /// <summary>
        /// Gets or sets the dependencies.
        /// </summary>
        /// <value>The dependencies.</value>
        [DescriptorProperty("dependencies")]
        public virtual IEnumerable<string> Dependencies { get; set; }

        /// <summary>
        /// Gets or sets the descriptor file.
        /// </summary>
        /// <value>The descriptor file.</value>
        public virtual string DescriptorFile { get; set; }

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        /// <value>The name of the file.</value>
        [DescriptorProperty("path", "archive", ".zip")]
        public virtual string FileName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is selected.
        /// </summary>
        /// <value><c>true</c> if this instance is selected; otherwise, <c>false</c>.</value>
        public virtual bool IsSelected { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [DescriptorProperty("name")]
        public virtual string Name { get; set; }

        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        /// <value>The order.</value>
        public virtual int Order { get; set; }

        /// <summary>
        /// Gets or sets the picture.
        /// </summary>
        /// <value>The picture.</value>
        [DescriptorProperty("picture")]
        public virtual string Picture { get; set; }

        /// <summary>
        /// Gets or sets the remote identifier.
        /// </summary>
        /// <value>The remote identifier.</value>
        [DescriptorProperty("remote_file_id")]
        public virtual int? RemoteId { get; set; }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>The source.</value>
        public virtual ModSource Source { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        [DescriptorProperty("tags")]
        public virtual IEnumerable<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        [DescriptorProperty("supported_version")]
        public virtual string Version { get; set; }

        #endregion Properties
    }
}