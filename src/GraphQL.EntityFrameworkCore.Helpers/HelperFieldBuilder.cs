using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GraphQL.Resolvers;
using GraphQL.Subscription;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty>
    {
        public EventStreamFieldType FieldType { get; }
        
        public HelperFieldBuilder(EventStreamFieldType fieldType)
        {
            FieldType = fieldType;
        }

        public HelperFieldBuilder<TSourceType, TReturnType, TProperty> ThenTo<TProperty>(
            Expression<Func<TPreviousProperty, IEnumerable<TProperty>>> accessor)
        {
            var type = typeof(TSourceType);
            var property = FieldHelpers.GetPropertyInfo(accessor);

            FieldHelpers.Map(type, FieldType, property);

            return new HelperFieldBuilder<TSourceType, TReturnType, TProperty>(FieldType);
        }

        public HelperFieldBuilder<TSourceType, TReturnType, TProperty> ThenTo<TProperty>(
            Expression<Func<TPreviousProperty, TProperty>> accessor)
        {
            var type = typeof(TSourceType);
            var property = FieldHelpers.GetPropertyInfo(accessor);

            FieldHelpers.Map(type, FieldType, property);

            return new HelperFieldBuilder<TSourceType, TReturnType, TProperty>(FieldType);
        }

        public virtual HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty> Type(IGraphType type)
        {
            FieldType.ResolvedType = type;
            return this;
        }

        public virtual HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty> Name(string name)
        {
            NameValidator.ValidateName(name);

            FieldType.Name = name;
            return this;
        }

        public virtual HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty> Description(string description)
        {
            FieldType.Description = description;
            return this;
        }

        public virtual HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty> DeprecationReason(string deprecationReason)
        {
            FieldType.DeprecationReason = deprecationReason;
            return this;
        }

        public virtual HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty> DefaultValue(TReturnType defaultValue = default)
        {
            FieldType.DefaultValue = defaultValue;
            return this;
        }

        internal HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty> DefaultValue(object defaultValue)
        {
            FieldType.DefaultValue = defaultValue;
            return this;
        }

        public virtual HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty> Resolve(IFieldResolver resolver)
        {
            FieldType.Resolver = resolver;
            return this;
        }

        public virtual HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty> Resolve(Func<IResolveFieldContext<TSourceType>, TReturnType> resolve)
            => Resolve(new FuncFieldResolver<TSourceType, TReturnType>(resolve));

        public virtual HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty> ResolveAsync(Func<IResolveFieldContext<TSourceType>, Task<TReturnType>> resolve)
            => Resolve(new AsyncFieldResolver<TSourceType, TReturnType>(resolve));

        public virtual HelperFieldBuilder<TSourceType, TNewReturnType, TPreviousProperty> Returns<TNewReturnType>()
            => new HelperFieldBuilder<TSourceType, TNewReturnType, TPreviousProperty>(FieldType);

        public virtual HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty> Argument<TArgumentGraphType>(string name, string description, Action<QueryArgument> configure = null)
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                configure?.Invoke(arg);
            });

        public virtual HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty> Argument<TArgumentGraphType, TArgumentType>(string name, string description,
            TArgumentType defaultValue = default, Action<QueryArgument> configure = null)
            => Argument<TArgumentGraphType>(name, arg =>
            {
                arg.Description = description;
                arg.DefaultValue = defaultValue;
                configure?.Invoke(arg);
            });

        public virtual HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty> Argument<TArgumentGraphType>(string name, Action<QueryArgument> configure = null)
        {
            var arg = new QueryArgument(typeof(TArgumentGraphType))
            {
                Name = name,
            };
            configure?.Invoke(arg);
            FieldType.Arguments.Add(arg);
            return this;
        }

        public virtual HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty> Configure(Action<FieldType> configure)
        {
            configure(FieldType);
            return this;
        }

        public virtual HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty> Subscribe(Func<IResolveEventStreamContext<TSourceType>, IObservable<TReturnType>> subscribe)
        {
            FieldType.Subscriber = new EventStreamResolver<TSourceType, TReturnType>(subscribe);
            return this;
        }

        public virtual HelperFieldBuilder<TSourceType, TReturnType, TPreviousProperty> SubscribeAsync(Func<IResolveEventStreamContext<TSourceType>, Task<IObservable<TReturnType>>> subscribeAsync)
        {
            FieldType.AsyncSubscriber = new AsyncEventStreamResolver<TSourceType, TReturnType>(subscribeAsync);
            return this;
        }
    }
}