﻿// ***********************************************************************
// Assembly         : IronyModManager.Parser
// Author           : Mario
// Created          : 02-18-2020
//
// Last Modified By : Mario
// Last Modified On : 02-25-2020
// ***********************************************************************
// <copyright file="LocalizationParser.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using IronyModManager.Parser.Common.Args;
using IronyModManager.Parser.Common.Definitions;
using IronyModManager.Parser.Common.Parsers;

namespace IronyModManager.Parser.Generic
{
    /// <summary>
    /// Class LocalizationParser.
    /// Implements the <see cref="IronyModManager.Parser.Common.Parsers.BaseParser" />
    /// Implements the <see cref="IronyModManager.Parser.Common.Parsers.IGenericParser" />
    /// </summary>
    /// <seealso cref="IronyModManager.Parser.Common.Parsers.BaseParser" />
    /// <seealso cref="IronyModManager.Parser.Common.Parsers.IGenericParser" />
    public class LocalizationParser : BaseParser, IGenericParser
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizationParser" /> class.
        /// </summary>
        /// <param name="textParser">The text parser.</param>
        public LocalizationParser(ITextParser textParser) : base(textParser)
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Determines whether this instance can parse the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns><c>true</c> if this instance can parse the specified arguments; otherwise, <c>false</c>.</returns>
        public bool CanParse(CanParseArgs args)
        {
            return args.File.EndsWith(Common.Constants.LocalizationExtension, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Parses the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>IEnumerable&lt;IDefinition&gt;.</returns>
        public override IEnumerable<IDefinition> Parse(ParserArgs args)
        {
            var result = new List<IDefinition>();
            string selectedLanguage = string.Empty;
            foreach (var line in args.Lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith(Common.Constants.Scripts.ScriptCommentId))
                {
                    continue;
                }
                var lang = GetLanguageId(line);
                if (!string.IsNullOrWhiteSpace(lang))
                {
                    selectedLanguage = lang;
                }
                if (!string.IsNullOrWhiteSpace(selectedLanguage))
                {
                    if (string.IsNullOrWhiteSpace(lang))
                    {
                        var def = GetDefinitionInstance();
                        var parsingArgs = ConstructArgs(args, def);
                        MapDefinitionFromArgs(parsingArgs);
                        def.Code = $"{selectedLanguage}:{Environment.NewLine}{line}";
                        def.Type = FormatType(args.File, $"{selectedLanguage}-{Common.Constants.YmlType}");
                        def.Id = textParser.GetKey(line, Common.Constants.Localization.YmlSeparator.ToString());
                        def.ValueType = Common.ValueType.Variable;
                        result.Add(def);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the language identifier.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>System.String.</returns>
        protected virtual string GetLanguageId(string line)
        {
            var lang = Common.Constants.Localization.Locales.FirstOrDefault(s => line.StartsWith(s, StringComparison.OrdinalIgnoreCase));
            return lang;
        }

        #endregion Methods
    }
}