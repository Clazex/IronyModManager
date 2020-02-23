﻿// ***********************************************************************
// Assembly         : IronyModManager.Parser
// Author           : Mario
// Created          : 02-22-2020
//
// Last Modified By : Mario
// Last Modified On : 02-22-2020
// ***********************************************************************
// <copyright file="ModParser.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using IronyModManager.DI;

namespace IronyModManager.Parser.Mod
{
    /// <summary>
    /// Class ModParser.
    /// Implements the <see cref="IronyModManager.Parser.Mod.IModParser" />
    /// </summary>
    /// <seealso cref="IronyModManager.Parser.Mod.IModParser" />
    public class ModParser : IModParser
    {
        #region Fields

        /// <summary>
        /// The text parser
        /// </summary>
        private readonly ITextParser textParser;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ModParser" /> class.
        /// </summary>
        /// <param name="textParser">The text parser.</param>
        public ModParser(ITextParser textParser)
        {
            this.textParser = textParser;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Parses the specified lines.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <returns>IModObject.</returns>
        public IModObject Parse(IEnumerable<string> lines)
        {
            var obj = DIResolver.Get<IModObject>();
            List<string> arrayProps = null;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(Constants.Scripts.ScriptCommentId))
                {
                    continue;
                }
                var cleaned = textParser.CleanWhitespace(line);
                if (cleaned.Contains(Constants.Scripts.VariableSeparatorId))
                {

                    var key = textParser.GetKey(cleaned, Constants.Scripts.VariableSeparatorId);
                    switch (key)
                    {
                        case "path":
                        case "archive":
                            obj.FileName = textParser.GetValue(cleaned, $"{key}{Constants.Scripts.VariableSeparatorId}");
                            break;

                        case "picture":
                            obj.Picture = textParser.GetValue(cleaned, $"{key}{Constants.Scripts.VariableSeparatorId}");
                            break;

                        case "name":
                            obj.Name = textParser.GetValue(cleaned, $"{key}{Constants.Scripts.VariableSeparatorId}");
                            break;

                        case "version":
                            obj.Version = textParser.GetValue(cleaned, $"{key}{Constants.Scripts.VariableSeparatorId}");
                            break;

                        case "supported_version":
                            obj.SupportedVersion = textParser.GetValue(cleaned, $"{key}{Constants.Scripts.VariableSeparatorId}");
                            break;

                        case "tags":
                            obj.Tags = arrayProps = new List<string>();
                            break;

                        case "remote_file_id":
                            if (int.TryParse(textParser.GetValue(cleaned, $"{key}{Constants.Scripts.VariableSeparatorId}"), out int value))
                            {
                                obj.RemoteId = value;
                            }
                            break;

                        case "dependencies":
                            obj.Dependencies = arrayProps = new List<string>();
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    if (arrayProps != null)
                    {
                        if (cleaned.Contains(Constants.Scripts.ClosingBracket))
                        {
                            arrayProps = null;
                        }
                        else
                        {
                            arrayProps.Add(cleaned.Replace("\"", string.Empty));
                        }
                    }
                }
            }
            return obj;
        }

        #endregion Methods
    }
}