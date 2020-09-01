﻿// ***********************************************************************
// Assembly         : IronyModManager.Parser
// Author           : Mario
// Created          : 02-22-2020
//
// Last Modified By : Mario
// Last Modified On : 09-01-2020
// ***********************************************************************
// <copyright file="CodeParser.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CWTools.CSharp;
using IronyModManager.DI;
using IronyModManager.Parser.Common.Parsers;
using IronyModManager.Parser.Common.Parsers.Models;
using IronyModManager.Shared;

namespace IronyModManager.Parser
{
    /// <summary>
    /// Class TextParser.
    /// Implements the <see cref="IronyModManager.Parser.Common.Parsers.ICodeParser" />
    /// </summary>
    /// <seealso cref="IronyModManager.Parser.Common.Parsers.ICodeParser" />
    [ExcludeFromCoverage("Code parser is tested in parser implementations.")]
    public class CodeParser : ICodeParser
    {
        #region Fields

        /// <summary>
        /// The cleaner conversion map
        /// </summary>
        protected static readonly Dictionary<string, string> cleanerConversionMap = new Dictionary<string, string>()
        {
            { $" {Common.Constants.Scripts.EqualsOperator}", Common.Constants.Scripts.EqualsOperator.ToString() },
            { $"{Common.Constants.Scripts.EqualsOperator} ", Common.Constants.Scripts.EqualsOperator.ToString() },
            { $" {Common.Constants.Scripts.OpenObject}", Common.Constants.Scripts.OpenObject.ToString() },
            { $"{Common.Constants.Scripts.OpenObject} ", Common.Constants.Scripts.OpenObject.ToString() },
            { $" {Common.Constants.Scripts.CloseObject}", Common.Constants.Scripts.CloseObject.ToString() },
            { $"{Common.Constants.Scripts.CloseObject} ", Common.Constants.Scripts.CloseObject.ToString() },
        };

        #endregion Fields

        #region Methods

        /// <summary>
        /// Cleans the whitespace.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>System.String.</returns>
        public string CleanWhitespace(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return string.Empty;
            }
            var cleaned = string.Join(' ', line.Trim().Replace("\t", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries));
            foreach (var item in cleanerConversionMap)
            {
                cleaned = cleaned.Replace(item.Key, item.Value);
            }
            return cleaned;
        }

        /// <summary>
        /// Formats the code.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="indentLevel">The indent level.</param>
        /// <returns>System.String.</returns>
        public string FormatCode(IScriptElement element, int indentLevel = 0)
        {
            static string format(IScriptElement element, int indent)
            {
                var sb = new StringBuilder();
                if (element.IsSimpleType)
                {
                    if (!string.IsNullOrWhiteSpace(element.Value))
                    {
                        sb.Append($"{new string(' ', indent * 4)}{element.Key} {element.Operator} {element.Value}");
                    }
                    else
                    {
                        sb.Append($"{new string(' ', indent * 4)}{element.Key}");
                    }
                }
                else
                {
                    sb.AppendLine($"{new string(' ', indent * 4)}{element.Key} {element.Operator} {Common.Constants.Scripts.OpenObject}");
                    if (element.Values?.Count() > 0)
                    {
                        foreach (var value in element.Values)
                        {
                            sb.AppendLine(format(value, indent + 1));
                        }
                    }
                    sb.Append($"{new string(' ', indent * 4)}{Common.Constants.Scripts.CloseObject}");
                }
                return sb.ToString();
            }
            if (element != null)
            {
                return format(element, indentLevel);
            }
            return string.Empty;
        }

        /// <summary>
        /// Parses the script.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <param name="file">The file.</param>
        /// <param name="performSimpleCheck">if set to <c>true</c> [perform simple check].</param>
        /// <returns>IParseResponse.</returns>
        public IParseResponse ParseScript(IEnumerable<string> lines, string file, bool performSimpleCheck = false)
        {
            return ParseScriptData(lines, file, performSimpleCheck, false);
        }

        /// <summary>
        /// Parses the script without validation.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <returns>IParseResponse.</returns>
        public IParseResponse ParseScriptWithoutValidation(IEnumerable<string> lines)
        {
            return ParseScriptData(lines, skipValidityCheck: true);
        }

        /// <summary>
        /// Performs the validity check.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <param name="file">The file.</param>
        /// <param name="performSimpleCheck">if set to <c>true</c> [perform simple check].</param>
        /// <returns>IScriptError.</returns>
        public IScriptError PerformValidityCheck(IEnumerable<string> lines, string file, bool performSimpleCheck = false)
        {
            if (performSimpleCheck)
            {
                var error = PerformBasicValidityCheck(lines);
                if (error != null)
                {
                    return error;
                }
            }
            else
            {
                var code = string.Join(Environment.NewLine, lines);
                var response = Parsers.ParseScriptFile(file, code);
                if (!response.IsSuccess)
                {
                    var errorResponse = response.GetError();
                    var error = DIResolver.Get<IScriptError>();
                    error.Column = errorResponse.Column;
                    error.Line = errorResponse.Line;
                    error.Message = errorResponse.ErrorMessage;
                    return error;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the element.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="index">The index.</param>
        /// <returns>IScriptElement.</returns>
        protected IScriptElement GetElement(List<char> code, ref int index)
        {
            IgnoreElementWhiteSpace(code, ref index);
            var elKey = GetElementValue(code, ref index, true);
            char? elOperator;
            if (!string.IsNullOrWhiteSpace(elKey.Operator))
            {
                elOperator = elKey.Operator[0];
                if (elKey.Operator.Count() > 1)
                {
                    // Move back index in case we're seing greater or equal to kind of operators
                    index -= elKey.Operator.Count() - 1;
                }
            }
            else
            {
                IgnoreElementWhiteSpace(code, ref index);
                elOperator = GetElementCharacter(code, index);
            }
            if (!Common.Constants.Scripts.Operators.Any(p => p == elOperator))
            {
                if (!string.IsNullOrWhiteSpace(elKey.Value))
                {
                    var scriptElement = DIResolver.Get<IScriptElement>();
                    scriptElement.Key = elKey.Value;
                    scriptElement.IsSimpleType = true;
                    return scriptElement;
                }
                else
                {
                    return null;
                }
            }
            IgnoreElementWhiteSpace(code, ref index);
            var elValue = GetElementValue(code, ref index);
            if (elValue.Terminator.HasValue)
            {
                if (elValue.Terminator.GetValueOrDefault() != Common.Constants.Scripts.OpenObject)
                {
                    return null;
                }
                index++;
                var values = GetElements(code, ref index);
                var scriptElement = DIResolver.Get<IScriptElement>();
                scriptElement.Key = elKey.Value;
                scriptElement.Values = values;
                scriptElement.Operator = Common.Constants.Scripts.EqualsOperator.ToString();
                return scriptElement;
            }
            else if (Common.Constants.Scripts.InlineOperators.Any(p => p.Equals(elValue.Value, StringComparison.OrdinalIgnoreCase)))
            {
                IgnoreElementWhiteSpace(code, ref index);
                var colorValues = GetElementValue(code, ref index);
                if (colorValues.Terminator.HasValue)
                {
                    index++;
                    var values = GetElements(code, ref index);
                    var parentElement = DIResolver.Get<IScriptElement>();
                    parentElement.Key = elKey.Value;
                    parentElement.Operator = Common.Constants.Scripts.EqualsOperator.ToString();
                    var childElement = DIResolver.Get<IScriptElement>();
                    childElement.Key = elValue.Value;
                    childElement.Values = values;
                    parentElement.Values = new List<IScriptElement>() { childElement };
                    return parentElement;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var scriptElement = DIResolver.Get<IScriptElement>();
                scriptElement.Key = elKey.Value;
                scriptElement.Value = elValue.Value;
                scriptElement.IsSimpleType = true;
                scriptElement.Operator = elValue.Operator;
                return scriptElement;
            }
        }

        /// <summary>
        /// Gets the element character.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="index">The index.</param>
        /// <returns>System.Nullable&lt;System.Char&gt;.</returns>
        protected char? GetElementCharacter(IList<char> code, int index)
        {
            char? character = null;
            if (index < code.Count())
            {
                character = code[index];
            }
            return character;
        }

        /// <summary>
        /// Gets the elements.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="index">The index.</param>
        /// <returns>IEnumerable&lt;IScriptElement&gt;.</returns>
        protected IEnumerable<IScriptElement> GetElements(List<char> code, ref int index)
        {
            var values = new List<IScriptElement>();
            for (int i = index; i < code.Count(); i++)
            {
                var character = GetElementCharacter(code, i);
                if (character == null || character == Common.Constants.Scripts.CloseObject)
                {
                    index = i + 1;
                    break;
                }
                var el = GetElement(code, ref i);
                if (el != null)
                {
                    values.Add(el);
                }
                // Move position back by
                i = index = i - 1;
            }
            return values;
        }

        /// <summary>
        /// Gets the element value.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="index">The index.</param>
        /// <param name="breakOnOperatorTerminator">if set to <c>true</c> [break on operator terminator].</param>
        /// <returns>ElementValue.</returns>
        protected ElementValue GetElementValue(List<char> code, ref int index, bool breakOnOperatorTerminator = false)
        {
            char? terminator = null;
            var sbValue = new StringBuilder();
            var sbOperator = new StringBuilder();
            var openQuote = false;
            var operatorOpened = false;
            for (int i = index; i < code.Count(); i++)
            {
                var character = GetElementCharacter(code, i);
                if (character == null)
                {
                    index = i;
                    break;
                }
                if (character == Common.Constants.Scripts.Quote)
                {
                    if (operatorOpened)
                    {
                        if (breakOnOperatorTerminator)
                        {
                            break;
                        }
                        else
                        {
                            operatorOpened = false;
                        }
                    }
                    if (openQuote)
                    {
                        sbValue.Append(character.GetValueOrDefault());
                        index = i + 1;
                        break;
                    }
                    openQuote = true;
                }
                else if (Common.Constants.Scripts.Operators.Any(p => p == character.GetValueOrDefault()))
                {
                    if (!openQuote)
                    {
                        operatorOpened = true;
                    }
                }
                else if (char.IsWhiteSpace(character.GetValueOrDefault()) && !openQuote)
                {
                    if (!operatorOpened)
                    {
                        index = i;
                        break;
                    }
                    else
                    {
                        if (breakOnOperatorTerminator)
                        {
                            break;
                        }
                        else
                        {
                            sbOperator.Append(character.GetValueOrDefault());
                            IgnoreElementWhiteSpace(code, ref i);
                            // Move back by 1
                            i--;
                            operatorOpened = false;
                            index = i;
                            continue;
                        }
                    }
                }
                else if (Common.Constants.Scripts.CodeTerminators.Any(p => p == character.GetValueOrDefault()))
                {
                    terminator = character;
                    index = i;
                    break;
                }
                else if (!char.IsWhiteSpace(character.GetValueOrDefault()))
                {
                    if (operatorOpened)
                    {
                        if (breakOnOperatorTerminator)
                        {
                            break;
                        }
                        else
                        {
                            operatorOpened = false;
                        }
                    }
                }
                if (!operatorOpened)
                {
                    sbValue.Append(character.GetValueOrDefault());
                }
                else
                {
                    sbOperator.Append(character.GetValueOrDefault());
                }
                index = i;
            }
            return new ElementValue()
            {
                Value = sbValue.ToString(),
                Operator = sbOperator.ToString().Trim(),
                Terminator = terminator
            };
        }

        /// <summary>
        /// Ignores the element white space.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="index">The index.</param>
        protected void IgnoreElementWhiteSpace(List<char> code, ref int index)
        {
            for (int i = index; i < code.Count(); i++)
            {
                var character = GetElementCharacter(code, i);
                if (character == null || char.IsWhiteSpace(character.GetValueOrDefault()))
                {
                    continue;
                }
                else
                {
                    index = i;
                    break;
                }
            }
        }

        /// <summary>
        /// Parses the elements.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <returns>IEnumerable&lt;IScriptElement&gt;.</returns>
        protected IEnumerable<IScriptElement> ParseElements(IEnumerable<string> lines)
        {
            var result = new List<IScriptElement>();
            var validCodeLines = lines.Where(p => !string.IsNullOrWhiteSpace(p) && !p.Trim().StartsWith(Common.Constants.Scripts.ScriptCommentId.ToString()))
                .Select(p => p.IndexOf(Common.Constants.Scripts.ScriptCommentId) > 0 ? p.Substring(0, p.IndexOf(Common.Constants.Scripts.ScriptCommentId) - 1) : p);
            var code = string.Join(Environment.NewLine, validCodeLines).ToList();
            for (int i = 0; i < code.Count(); i++)
            {
                var element = GetElement(code, ref i);
                if (element != null)
                {
                    result.Add(element);
                }
            }
            return result;
        }

        /// <summary>
        /// Parses the script data.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <param name="file">The file.</param>
        /// <param name="performSimpleCheck">if set to <c>true</c> [perform simple check].</param>
        /// <param name="skipValidityCheck">if set to <c>true</c> [skip validity check].</param>
        /// <returns>IParseResponse.</returns>
        protected IParseResponse ParseScriptData(IEnumerable<string> lines, string file = Constants.EmptyParam, bool performSimpleCheck = false, bool skipValidityCheck = false)
        {
            var result = DIResolver.Get<IParseResponse>();
            if (skipValidityCheck)
            {
                result.Values = ParseElements(lines);
            }
            else
            {
                var error = PerformValidityCheck(lines, file, performSimpleCheck);
                if (error != null)
                {
                    result.Error = error;
                }
                else
                {
                    result.Values = ParseElements(lines);
                }
            }
            return result;
        }

        /// <summary>
        /// Performs the basic validity check.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <returns>IScriptError.</returns>
        protected IScriptError PerformBasicValidityCheck(IEnumerable<string> lines)
        {
            var text = string.Join(Environment.NewLine, lines);
            var openBracket = text.Count(s => s == Common.Constants.Scripts.OpenObject);
            var closeBracket = text.Count(s => s == Common.Constants.Scripts.CloseObject);
            if (openBracket != closeBracket)
            {
                var error = DIResolver.Get<IScriptError>();
                error.Message = "Number of open and close curly brackets does not match. This indicates a syntax error somewhere in the file.";
                return error;
            }
            return null;
        }

        #endregion Methods

        #region Classes

        /// <summary>
        /// Class ElementValue.
        /// </summary>
        protected class ElementValue
        {
            #region Properties

            /// <summary>
            /// Gets or sets the operator.
            /// </summary>
            /// <value>The operator.</value>
            public string Operator { get; set; }

            /// <summary>
            /// Gets or sets the terminator.
            /// </summary>
            /// <value>The terminator.</value>
            public char? Terminator { get; set; }

            /// <summary>
            /// Gets or sets the value.
            /// </summary>
            /// <value>The value.</value>
            public string Value { get; set; }

            #endregion Properties
        }

        #endregion Classes
    }
}
