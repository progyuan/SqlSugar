﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
namespace SqlSugar
{
    public partial class SqlSugarAccessory
    {
        public SqlSugarClient Context { get; set; }
        public string EntityNamespace { get; set; }
        public IConnectionConfig CurrentConnectionConfig { get; set; }
        public Dictionary<string, object> TempItems { get; set; }
        public Guid ContextID { get; set; }
        public MappingTableList MappingTables = new MappingTableList();
        public MappingColumnList MappingColumns = new MappingColumnList();
        public IgnoreComumnList IgnoreColumns = new IgnoreComumnList();

        protected ISqlBuilder _SqlBuilder;
        protected EntityProvider _EntityProvider;
        protected IAdo _Ado;
        protected ILambdaExpressions _LambdaExpressions;
        protected IRewritableMethods _RewritableMethods;
        protected IDbFirst _DbFirst;
        protected ICodeFirst _CodeFirst;
        protected IDbMaintenance _DbMaintenance;


        protected void InitMppingInfo<T, T2, T3, T4>()
        {
            InitMppingInfo<T, T2, T3>();
            InitMppingInfo<T4>();
        }

        protected void InitMppingInfo<T, T2, T3>()
        {
            InitMppingInfo<T, T2>();
            InitMppingInfo<T3>();
        }

        protected void InitMppingInfo<T, T2>()
        {
            InitMppingInfo<T>();
            InitMppingInfo<T2>();
        }

        protected void InitMppingInfo<T>()
        {
            string cacheKey = "Context.InitAttributeMappingTables";
            CacheFactory.Action<EntityInfo>(cacheKey,
             (cm, key) =>
             {
                 var cacheInfo = cm[key];
                 InitMppingInfo(cacheInfo);
             },
             (cm, key) =>
             {
                 var reval = this.Context.EntityProvider.GetEntityInfo<T>();
                 InitMppingInfo(reval);
                 return reval;
             });
        }
        private void InitMppingInfo(EntityInfo entityInfo)
        {
            if (this.MappingTables == null)
                this.MappingTables = new MappingTableList();
            if (this.MappingColumns == null)
                this.MappingColumns = new MappingColumnList();
            if (this.IgnoreColumns == null)
                this.IgnoreColumns = new IgnoreComumnList();
            if (!this.MappingTables.Any(it => it.EntityName == entityInfo.EntityName))
            {
                if (entityInfo.DbTableName != entityInfo.EntityName)
                {
                    this.MappingTables.Add(entityInfo.EntityName, entityInfo.DbTableName);
                }
            }
            if (entityInfo.Columns.Any(it => it.EntityName == entityInfo.EntityName))
            {
                var mappingColumnInfos = this.MappingColumns.Where(it => it.EntityName == entityInfo.EntityName);
                foreach (var item in entityInfo.Columns.Where(it=>it.IsIgnore==false))
                {
                    if (!mappingColumnInfos.Any(it => it.PropertyName == item.PropertyName))
                        if (item.PropertyName != item.DbColumnName && item.DbColumnName.IsValuable())
                            this.MappingColumns.Add(item.PropertyName, item.DbColumnName, item.EntityName);
                }
                var ignoreInfos = this.IgnoreColumns.Where(it => it.EntityName == entityInfo.EntityName);
                foreach (var item in entityInfo.Columns.Where(it=>it.IsIgnore))
                {
                    if (!ignoreInfos.Any(it => it.PropertyName == item.PropertyName))
                            this.IgnoreColumns.Add(item.PropertyName, item.EntityName);
                }
            }
        }
        protected List<JoinQueryInfo> GetJoinInfos(Expression joinExpression, ref string shortName, params Type[] entityTypeArray)
        {
            List<JoinQueryInfo> reval = new List<JoinQueryInfo>();
            var lambdaParameters = ((LambdaExpression)joinExpression).Parameters.ToList();
            ExpressionContext exp = new ExpressionContext();
            exp.MappingColumns = this.Context.MappingColumns;
            exp.MappingTables = this.Context.MappingTables;
            exp.Resolve(joinExpression, ResolveExpressType.Join);
            int i = 0;
            var joinArray = exp.Result.GetResultArray();
            foreach (var type in entityTypeArray)
            {
                var isFirst = i == 0;
                ++i;
                JoinQueryInfo joinInfo = new JoinQueryInfo();
                var hasMappingTable = exp.MappingTables.IsValuable();
                MappingTable mappingInfo = null;
                if (hasMappingTable)
                {
                    mappingInfo = exp.MappingTables.FirstOrDefault(it => it.EntityName.Equals(type.Name, StringComparison.CurrentCultureIgnoreCase));
                    joinInfo.TableName = mappingInfo != null ? mappingInfo.DbTableName : type.Name;
                }
                else
                {
                    joinInfo.TableName = type.Name;
                }
                if (isFirst)
                {
                    var firstItem = lambdaParameters.First();
                    lambdaParameters.Remove(firstItem);
                    shortName = firstItem.Name;
                }
                var joinString = joinArray[i * 2 - 2];
                joinInfo.ShortName = lambdaParameters[i - 1].Name;
                joinInfo.JoinType = (JoinType)Enum.Parse(typeof(JoinType), joinString);
                joinInfo.JoinWhere = joinArray[i * 2 - 1];
                joinInfo.JoinIndex = i;
                reval.Add((joinInfo));
            }
            return reval;
        }

        protected void CreateQueryable<T>(ISugarQueryable<T> result) where T : class, new()
        {
            var sqlBuilder = InstanceFactory.GetSqlbuilder(CurrentConnectionConfig);
            result.Context = this.Context;
            result.SqlBuilder = sqlBuilder;
            result.SqlBuilder.QueryBuilder = InstanceFactory.GetQueryBuilder(CurrentConnectionConfig);
            result.SqlBuilder.QueryBuilder.Builder = sqlBuilder;
            result.SqlBuilder.Context = result.SqlBuilder.QueryBuilder.Context = this.Context;
            result.SqlBuilder.QueryBuilder.EntityType = typeof(T);
            result.SqlBuilder.QueryBuilder.EntityName = typeof(T).Name;
            result.SqlBuilder.QueryBuilder.LambdaExpressions = InstanceFactory.GetLambdaExpressions(CurrentConnectionConfig);
        }

    }
}
