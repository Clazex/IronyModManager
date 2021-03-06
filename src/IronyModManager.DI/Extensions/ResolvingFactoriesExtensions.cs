﻿// ***********************************************************************
// Assembly         : IronyModManager.DI
// Author           : SimpleInjector
// Created          : 01-10-2020
//
// Last Modified By : Mario
// Last Modified On : 01-18-2020
// ***********************************************************************
// <copyright file="ResolvingFactoriesExtensions.cs" company="SimpleInjector">
//     Copyright (c) SimpleInjector. All rights reserved.
// </copyright>
// <summary>Source: https://github.com/simpleinjector/SimpleInjector/blob/master/src/SimpleInjector.CodeSamples/ResolvingFactoriesExtensions.cs#L14</summary>
// ***********************************************************************
using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SimpleInjector;

namespace IronyModManager.DI.Extensions
{
    /// <summary>
    /// Class ResolvingFactoriesExtensions.
    /// </summary>
    internal static class ResolvingFactoriesExtensions
    {
        #region Methods

        /// <summary>
        /// This extension method is equivalent to the following registration, for each and every T:<br />container.RegisterSingleton&lt;Func&lt;T&gt;&gt;(() =&gt; container.GetInstance&lt;T&gt;());<br />This is useful for consumers that need to create multiple instances of a dependency.<br />This mimics the behavior of Autofac. In Autofac this behavior is default.
        /// </summary>
        /// <param name="container">The container.</param>
        public static void AllowResolvingFuncFactories(this Container container)
        {
            container.ResolveUnregisteredType += (sender, e) =>
            {
                if (e.UnregisteredServiceType.IsGenericType &&
                    e.UnregisteredServiceType.GetGenericTypeDefinition() == typeof(Func<>))
                {
                    var serviceType = e.UnregisteredServiceType.GetGenericArguments()[0];

                    var registration = container.GetRegistration(serviceType, true);

                    var funcType = typeof(Func<>).MakeGenericType(serviceType);

                    var factoryDelegate =
                        Expression.Lambda(funcType, registration.BuildExpression()).Compile();

                    e.Register(Expression.Constant(factoryDelegate));
                }
            };
        }

        /// <summary>
        /// This extension method is equivalent to the following registration, for each and every T:<br />container.Register&lt;Lazy&lt;T&gt;&gt;(() =&gt; new Lazy&lt;T&gt;(() =&gt; container.GetInstance&lt;T&gt;()));<br />This is useful for consumers that have a dependency on a service that is expensive to create, but<br />not always needed.<br />This mimics the behavior of Autofac and Ninject 3. In Autofac this behavior is default.
        /// </summary>
        /// <param name="container">The container.</param>
        public static void AllowResolvingLazyFactories(this Container container)
        {
            container.ResolveUnregisteredType += (sender, e) =>
            {
                if (e.UnregisteredServiceType.IsGenericType &&
                    e.UnregisteredServiceType.GetGenericTypeDefinition() == typeof(Lazy<>))
                {
                    var serviceType = e.UnregisteredServiceType.GetGenericArguments()[0];

                    var registration = container.GetRegistration(serviceType, true);

                    var funcType = typeof(Func<>).MakeGenericType(serviceType);
                    var lazyType = typeof(Lazy<>).MakeGenericType(serviceType);

                    var factoryDelegate = Expression.Lambda(funcType, registration.BuildExpression()).Compile();

                    var lazyConstructor = (
                        from ctor in lazyType.GetConstructors()
                        where ctor.GetParameters().Length == 1
                        where ctor.GetParameters()[0].ParameterType == funcType
                        select ctor)
                        .Single();

                    var expression = Expression.New(lazyConstructor, Expression.Constant(factoryDelegate));

                    var lazyRegistration = registration.Lifestyle.CreateRegistration(
                        serviceType: lazyType,
                        instanceCreator: Expression.Lambda<Func<object>>(expression).Compile(),
                        container: container);

                    e.Register(lazyRegistration);
                }
            };
        }

        /// <summary>
        /// Allows the resolving parameterized function factories.
        /// </summary>
        /// <param name="container">The container.</param>
        public static void AllowResolvingParameterizedFuncFactories(this Container container)
        {
            container.ResolveUnregisteredType += (sender, e) =>
            {
                if (!IsParameterizedFuncDelegate(e.UnregisteredServiceType))
                {
                    return;
                }

                Type[] genericArguments = e.UnregisteredServiceType.GetGenericArguments();

                var componentType = genericArguments.Last();

                if (componentType.IsAbstract)
                {
                    return;
                }

                var funcType = e.UnregisteredServiceType;

                var factoryArguments = genericArguments.Take(genericArguments.Length - 1).ToArray();

                var constructor = container.Options.ConstructorResolutionBehavior
                    .GetConstructor(componentType);

                var parameters = (
                    from factoryArgumentType in factoryArguments
                    select Expression.Parameter(factoryArgumentType))
                    .ToArray();

                var factoryDelegate = Expression.Lambda(funcType,
                    BuildNewExpression(container, constructor, parameters),
                    parameters)
                    .Compile();

                e.Register(Expression.Constant(factoryDelegate));
            };
        }

        /// <summary>
        /// Builds the new expression.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="constructor">The constructor.</param>
        /// <param name="funcParameterExpression">The function parameter expression.</param>
        /// <returns>NewExpression.</returns>
        /// <exception cref="ActivationException"></exception>
        private static NewExpression BuildNewExpression(Container container,
            ConstructorInfo constructor,
            ParameterExpression[] funcParameterExpression)
        {
            var ctorParameters = constructor.GetParameters();
            var ctorParameterTypes = ctorParameters.Select(p => p.ParameterType).ToArray();
            var funcParameterTypes = funcParameterExpression.Select(p => p.Type).ToArray();

            int funcParametersIndex = IndexOfSubCollection(ctorParameterTypes, funcParameterTypes);

            if (funcParametersIndex == -1)
            {
                throw new ActivationException(string.Format(CultureInfo.CurrentCulture,
                    "The constructor of type {0} did not contain the sequence of the following " +
                    "constructor parameters: {1}.",
                    constructor.DeclaringType.ToFriendlyName(),
                    string.Join(", ", funcParameterTypes.Select(t => t.ToFriendlyName()))));
            }

            var firstCtorParameterExpressions = ctorParameterTypes
                .Take(funcParametersIndex)
                .Select(type => container.GetRegistration(type, true).BuildExpression());

            var lastCtorParameterExpressions = ctorParameterTypes
                .Skip(funcParametersIndex + funcParameterTypes.Length)
                .Select(type => container.GetRegistration(type, true).BuildExpression());

            var expressions = firstCtorParameterExpressions
                .Concat(funcParameterExpression)
                .Concat(lastCtorParameterExpressions)
                .ToArray();

            return Expression.New(constructor, expressions);
        }

        /// <summary>
        /// Indexes the of sub collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="subCollection">The sub collection.</param>
        /// <returns>System.Int32.</returns>
        private static int IndexOfSubCollection(Type[] collection, Type[] subCollection)
        {
            return (
                from index in Enumerable.Range(0, collection.Length - subCollection.Length + 1)
                let collectionPart = collection.Skip(index).Take(subCollection.Length)
                where collectionPart.SequenceEqual(subCollection)
                select (int?)index)
                .FirstOrDefault() ?? -1;
        }

        /// <summary>
        /// Determines whether [is parameterized function delegate] [the specified type].
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if [is parameterized function delegate] [the specified type]; otherwise, <c>false</c>.</returns>
        private static bool IsParameterizedFuncDelegate(Type type)
        {
            if (!type.IsGenericType || !type.FullName.StartsWith("System.Func`"))
            {
                return false;
            }

            return type.GetGenericTypeDefinition().GetGenericArguments().Length > 1;
        }

        #endregion Methods
    }
}
