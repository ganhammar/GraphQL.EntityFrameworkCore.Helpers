using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Subscription;
using GraphQL.Types;
using GraphQL.Utilities;
using Microsoft.EntityFrameworkCore;

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

    public static class HelperFieldBuilderExtensions
    {
        public static CollectionBatchQueryBuilder<TSourceType, TReturnType, TDbContext, TProperty> Include<TSourceType, TReturnType, TDbContext, TProperty>(
                this HelperFieldBuilder<TSourceType, IEnumerable<TReturnType>, TProperty> field,
                TDbContext dbContext)
            where TDbContext : DbContext
        {
            return new CollectionBatchQueryBuilder<TSourceType, TReturnType, TDbContext, TProperty>(
                field, dbContext, FieldHelpers.GetPropertyPath(typeof(TSourceType), field.FieldType));
        }

        public static HelperFieldBuilder<TSourceType, TReturnType, TProperty> ResolveAsync<TSourceType, TReturnType, TProperty>(this HelperFieldBuilder<TSourceType, TReturnType, TProperty> builder, Func<IResolveFieldContext<TSourceType>, IDataLoaderResult<TReturnType>> resolve)
            => builder.Resolve(new FuncFieldResolver<TSourceType, IDataLoaderResult<TReturnType>>(resolve));

        public static HelperFieldBuilder<TSourceType, TReturnType, TProperty> ResolveAsync<TSourceType, TReturnType, TProperty>(this HelperFieldBuilder<TSourceType, TReturnType, TProperty> builder, Func<IResolveFieldContext<TSourceType>, Task<IDataLoaderResult<TReturnType>>> resolve)
            => builder.Resolve(new AsyncFieldResolver<TSourceType, IDataLoaderResult<TReturnType>>(resolve));

        // chained data loaders
        public static HelperFieldBuilder<TSourceType, TReturnType, TProperty> ResolveAsync<TSourceType, TReturnType, TProperty>(this HelperFieldBuilder<TSourceType, TReturnType, TProperty> builder, Func<IResolveFieldContext<TSourceType>, IDataLoaderResult<IDataLoaderResult<TReturnType>>> resolve)
            => builder.Resolve(new FuncFieldResolver<TSourceType, IDataLoaderResult<IDataLoaderResult<TReturnType>>>(resolve));

        public static HelperFieldBuilder<TSourceType, TReturnType, TProperty> ResolveAsync<TSourceType, TReturnType, TProperty>(this HelperFieldBuilder<TSourceType, TReturnType, TProperty> builder, Func<IResolveFieldContext<TSourceType>, Task<IDataLoaderResult<IDataLoaderResult<TReturnType>>>> resolve)
            => builder.Resolve(new AsyncFieldResolver<TSourceType, IDataLoaderResult<IDataLoaderResult<TReturnType>>>(resolve));

        // chain of 3 data loaders
        public static HelperFieldBuilder<TSourceType, TReturnType, TProperty> ResolveAsync<TSourceType, TReturnType, TProperty>(this HelperFieldBuilder<TSourceType, TReturnType, TProperty> builder, Func<IResolveFieldContext<TSourceType>, IDataLoaderResult<IDataLoaderResult<IDataLoaderResult<TReturnType>>>> resolve)
            => builder.Resolve(new FuncFieldResolver<TSourceType, IDataLoaderResult<IDataLoaderResult<IDataLoaderResult<TReturnType>>>>(resolve));

        public static HelperFieldBuilder<TSourceType, TReturnType, TProperty> ResolveAsync<TSourceType, TReturnType, TProperty>(this HelperFieldBuilder<TSourceType, TReturnType, TProperty> builder, Func<IResolveFieldContext<TSourceType>, Task<IDataLoaderResult<IDataLoaderResult<IDataLoaderResult<TReturnType>>>>> resolve)
            => builder.Resolve(new AsyncFieldResolver<TSourceType, IDataLoaderResult<IDataLoaderResult<IDataLoaderResult<TReturnType>>>>(resolve));
    }
}