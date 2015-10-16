﻿namespace Dapper.FastCrud.EntityDescriptors
{
    using System;
    using System.Collections.Generic;
    using Dapper.FastCrud.Mappings;
    using Dapper.FastCrud.SqlBuilders;
    using Dapper.FastCrud.SqlStatements;

    /// <summary>
    /// Typed entity descriptor, capable of producing statement builders associated with default entity mappings.
    /// </summary>
    internal class EntityDescriptor<TEntity>:EntityDescriptor
    {
        private volatile IDictionary<EntityMapping, ISqlStatements> _registeredEntityMappings;

        /// <summary>
        /// Default constructor
        /// </summary>
        public EntityDescriptor()
            :base(typeof(TEntity))
        {
            _registeredEntityMappings = new Dictionary<EntityMapping, ISqlStatements>();   
            this.DefaultEntityMapping = new AutoGeneratedEntityMapping<TEntity>(); 
        }

        /// <summary>
        /// Gets the registered entity mappings.
        /// </summary>
        public override IDictionary<EntityMapping, ISqlStatements> RegisteredEntityMappings
        {
            get
            {
                return _registeredEntityMappings;
            }
        }

        /// <summary>
        /// Returns the sql statements for an entity mapping, or the default one if the argument is null.
        /// </summary>
        public ISqlStatements GetSqlStatements(EntityMapping entityMapping = null)
        {
            entityMapping = entityMapping ?? DefaultEntityMapping;

            entityMapping.IsFrozen = true;

            ISqlStatements sqlStatements;
            IStatementSqlBuilder statementSqlBuilder;

            var originalRegisteredEntityMappings = _registeredEntityMappings;
            if (!originalRegisteredEntityMappings.TryGetValue(entityMapping, out sqlStatements))
            {
                switch (entityMapping.Dialect)
                {
                    case SqlDialect.MsSql:
                        statementSqlBuilder = new MsSqlBuilder(
                            OrmConfiguration.GetDialectConfiguration(SqlDialect.MsSql),
                            this,
                            entityMapping);
                        break;
                    case SqlDialect.MySql:
                        statementSqlBuilder = new MySqlBuilder(
                            OrmConfiguration.GetDialectConfiguration(SqlDialect.MySql),
                            this,
                            entityMapping);
                        break;
                    case SqlDialect.PostgreSql:
                        statementSqlBuilder = new PostgreSqlBuilder(
                            OrmConfiguration.GetDialectConfiguration(SqlDialect.PostgreSql),
                            this,
                            entityMapping);
                        break;
                    case SqlDialect.SqLite:
                        statementSqlBuilder = new SqLiteBuilder(
                            OrmConfiguration.GetDialectConfiguration(SqlDialect.SqLite),
                            this,
                            entityMapping);
                        break;
                    default:
                        throw new NotSupportedException($"Dialect {entityMapping.Dialect} is not supported");
                }

                sqlStatements = new GenericSqlStatements<TEntity>(statementSqlBuilder);

                // replace the original collection
                // rather than using a lock, we prefer the risk of missing a registration or two as they will get captured eventually
                _registeredEntityMappings = new Dictionary<EntityMapping, ISqlStatements>(originalRegisteredEntityMappings) {[entityMapping] = sqlStatements };
            }
            return sqlStatements;
        }

    }
}