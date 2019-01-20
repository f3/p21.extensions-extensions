using System;

namespace P21.Extensions.Supplemental.Exceptions
{
    /// <summary>
    /// Represents errors that occur during the processing of a <see cref="P21.Extensions.BusinessRule.Rule"/>
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public readonly string RuleName;

        public BusinessRuleException(string ruleName, string message)
            : this(ruleName, message, null)
        {
        }

        public BusinessRuleException(string ruleName, Exception innerException)
            : this(ruleName, string.Empty, innerException)
        {
        }

        public BusinessRuleException(string ruleName, string message, Exception innerException)
            : base(message, innerException)
        {
            RuleName = ruleName;
        }

        public override string Message
        {
            get { return $"[Rule Name={RuleName}] {base.Message}"; }
        }
    }

    /// <summary>
    /// Represents errors that occur during the processing of a <see cref="System.Data.DataTable"/>
    /// within a <see cref="P21.Extensions.BusinessRule.Rule"/>'s <see cref="P21.Extensions.BusinessRule.Rule.Data.Set">data set</see>.
    /// </summary>
    public sealed class RuleDataTableException : BusinessRuleException
    {
        public readonly string TableName;

        public RuleDataTableException(string ruleName, string tableName, string message, Exception innerException)
            : base(ruleName, message, innerException)
        {
            TableName = tableName;
        }

        public RuleDataTableException(string ruleName, string tableName, string message)
            : this(ruleName, tableName, message, null)
        {
        }

        public RuleDataTableException(string ruleName, string tableName)
            : this(ruleName, tableName, string.Empty)
        {
        }

        public override string Message
        {
            get { return $"[DataTable Name={TableName}] {base.Message}"; }
        }
    }

    /// <summary>
    /// Represents errors that occur during the processing of a <see cref="System.Data.DataTable"/>'s data field
    /// in a <see cref="P21.Extensions.BusinessRule.Rule"/>'s <see cref="P21.Extensions.BusinessRule.Rule.Data.Set">data set</see>.
    /// </summary>
    public sealed class RuleDataFieldException : RuleDataTableException
    {
        public readonly string FieldName;

        public RuleDataFieldException(string ruleName, string tableName, string fieldName, string message,
            Exception innerException)
            : base(ruleName, tableName, message, innerException)
        {
            FieldName = fieldName;
        }

        public RuleDataFieldException(string ruleName, string tableName, string fieldName, Exception innerException)
            : this(ruleName, tableName, fieldName, string.Empty, innerException)
        {
        }

        public RuleDataFieldException(string ruleName, string tableName, string fieldName, string message)
            : this(ruleName, tableName, fieldName, message, null)
        {
        }

        public RuleDataFieldException(string ruleName, string tableName, string fieldName)
            : this(ruleName, tableName, fieldName, string.Empty)
        {
        }

        public override string Message
        {
            get { return $"[Field Name={FieldName}] {base.Message}"; }
        }
    }
}