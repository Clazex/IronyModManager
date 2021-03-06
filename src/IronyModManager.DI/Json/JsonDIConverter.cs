﻿// ***********************************************************************
// Assembly         : IronyModManager.DI
// Author           : Mario
// Created          : 04-07-2020
//
// Last Modified By : Mario
// Last Modified On : 04-19-2020
// ***********************************************************************
// <copyright file="JsonDIConverter.cs" company="Mario">
//     Mario
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace IronyModManager.DI.Json
{
    /// <summary>
    /// Class JsonDIConverter.
    /// Implements the <see cref="Newtonsoft.Json.JsonConverter" />
    /// </summary>
    /// <seealso cref="Newtonsoft.Json.JsonConverter" />
    internal class JsonDIConverter : JsonConverter
    {
        #region Methods

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns><c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.</returns>
        public override bool CanConvert(Type objectType)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(objectType))
            {
                var registration = DIResolver.GetImplementationType(objectType);
                return registration != null;
            }
            return false;
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var instance = DIResolver.Get(objectType);
            serializer.Populate(reader, instance);
            return instance;
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        #endregion Methods
    }
}
