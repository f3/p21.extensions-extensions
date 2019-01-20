using System;
using System.Xml.Linq;
using System.Xml.XPath;

namespace P21.Extensions.Supplemental.Extensions
{
    using P21.Extensions.Supplemental.Exceptions;
    using Rule = P21.Extensions.BusinessRule.Rule;

    // Disable compiler warning about catching general exception types
    #pragma warning disable CA1031
    internal static class RuleDataExtensions
    {
        /// <summary>
        /// Triggered when any of the extension methods in this class throws an exception. 
        /// See the remarks for usage gotchas.
        /// </summary>
        /// <remarks>Be careful with subscriptions from object instances to this event,
        /// and <b>always</b> make sure to unsubscribe from it when finished with it. 
        /// Subscribing to a static event will root the subscribing instance and prevent 
        /// the garbage collector from ever collecting it. See the example.</remarks>
        /// <example>Usage from within a Business Rule&apos;s <c>Execute</c> method<br/>
        /// <code>private static void OnRuleDataAccessException(object sender, Exception e)
        /// {
        ///     // Handle the exception. Here, we are just going to log the exception:
        ///     ((Rule)sender).Log.AddAndPersist(e.ToString());
        /// }
        /// public override RuleResultData ExecuteRule()
        /// {
        ///     // subscribe
        ///     RuleDataExtensions.RuleDataAccessException += OnRuleDataAccessException;
        ///     
        ///     try
        ///     {
        ///         // More rule code here;
        ///         // ...
        ///     } finally {
        ///         // unsubscribe to avoid missed GC of this rule object
        ///         RuleDataExtensions.RuleDataAccessException -= OnRuleDataAccessException;
        ///     }
        /// }</code>
        /// </example>
        public static event EventHandler<BusinessRuleException> RuleDataAccessException
        {
            add { ruleDataAccessException += value; }
            remove { ruleDataAccessException -= value; }
        }
        private static EventHandler<Exception> ruleDataAccessException;

        /// <summary>
        /// Returns a <see cref="DataTable"/> for the given Business Rule class name
        /// </summary>
        /// <param name="rule">The multi-row <see cref="Rule"/> to get the table from</param>
        /// <param name="className">The name of the table to get</param>
        /// <param name="throwOnMissing">If <c>true</c>, throws a <see cref="RuleDataTableException"/> if the table is not found</param>
        /// <exception cref="RuleDataTableException">The requested rule table name was not found</exception>
        /// <returns>The requested <see cref="DataTable"/></returns>
        public static DataTable GetMultiRowRuleTable(this Rule rule, string className, bool throwOnMissing = false)
        {
            var table = rule.Data?.Set?.Tables[className];
            if (null == table && throwOnMissing)
                throw new RuleDataTableException(rule.GetName(), $"Required Rule data table (className = {className}) is missing");

            return table;
        }

        /// <summary>
        /// Returns the value of a given class' field
        /// </summary>
        /// <param name="rule">The <see cref="Rule"/> to get the field value from</param>
        /// <param name="className">The name of the class/table with the field value</param>
        /// <param name="columnName">The field/column name</param>
        /// <param name="throwOnMissing">If <c>true</c>, throws an exception if the table or field are not found, or if any other error occurs</param>
        /// <param name="defaultValue">A default value for the field if not found (and <paramref name="throwOnError"/> is <c>false</c>)</param>
        /// <param name="rowNumber">The row number where the value can be found (multi-row only; see remarks)</param>
        /// <returns>A <c>string</c> containing the field's value, or the <paramref name="defaultValue"/> if not found (and <paramref name="throwOnError"/> is <c>false</c>)</returns>
        /// <remarks>For multi-row fields, the <paramref name="rowNumber"/> is the <see cref="DataRow"/>'s zero-indexed row number; otherwise the field's rowId</remarks>
        public static string GetFieldValue(this Rule rule, string className, string columnName, bool throwOnError = false, string defaultValue = null, int? rowNumber = null)
        {
            try
            {
                if (rule.RuleState.MultiRow)
                {
                    if (null == rule.Data)
                        throw new BusinessRuleException(rule.GetName(), new NullReferenceException("Rule data is empty!"));

                    var ruleTable = rule.GetMultiRowRuleTable(className, throwOnError);

                    if (!(ruleTable?.Columns?.Contains(columnName) ?? false))
                        throw new RuleDataFieldException(rule.GetName(), className, columnName,
                            $"Rule data table {className} does not contain the required column '{columnName}'.");

                    try
                    {
                        return rule?.Data?.Set?.Tables[className].Rows?[(rowNumber ?? 0)]?.Field<string>(columnName) ?? defaultValue;
                    }
                    catch(Exception)
                    {
                        if (throwOnError)
                            throw;
                    }
                }
                else
                {
                    if (!rule.Data.Fields.OfType<DataField>().Any(df =>
                            df.ClassName.Equals(className, StringComparison.OrdinalIgnoreCase) &&
                            df.FieldName.Equals(columnName, StringComparison.OrdinalIgnoreCase)) && throwOnError)
                        throw new BusinessRuleException(rule.GetName(),
                            new ArgumentException(
                                $"Rule data does not contain the required class '{className}', or the class does not contain the required field '{columnName}'."));

                    if (null == rowNumber)
                        rowNumber = rule.Data.GetActiveRowIDForTable(className);

                    try
                    {
                        return rule?.Data?.Fields[className, columnName, rowNumber.ToString()]?.FieldValue ?? defaultValue;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            catch (Exception e)
            {
                ruleDataAccessError?.Invoke(rule, e);
                if (throwOnError)
                    throw;
            }

            return defaultValue;
        }

        /// <summary>
        /// Returns a <see cref="XDocument"/> from the given <see cref="Rule"/>'s <see cref="Rule.XmlData"/>.
        /// </summary>
        /// <param name="rule">The DynaChange&#8482; Business <see cref="Rule"/></param>
        /// <returns>An <see cref="XDocument"/> representing the Business Rule XML data (rooted at <c>business_rule_extensions_xml</c>)</returns>
        public static XDocument GetRuleDocument(this Rule businessRule)
        {
            return XDocument.Parse(businessRule.XmlData);
        }

        /// <summary>
        /// Returns the value for a given field from the contents of an <see cref="XDocument"/>
        /// created from the <see cref="P21.Extensions.BusinessRule.Rule.XmlData"/>.
        /// </summary>
        /// <param name="ruleDocument">An <see cref="XDocument"/> representing the Business Rule XML data</param>
        /// <param name="className">The &quot;class name&quot; (table name) where the data field is located</param>
        /// <param name="fieldName">The column name containing the value to return (see Remarks)</param>
        /// <param name="rowId">The <c>rowID</c> of the value (defaults to <c>1</c>)</param>
        /// <param name="defaultValue">A value to return as the default, if the <paramref name="className"/> and/or <paramref name="fieldName"/> are not found</param>
        /// <returns>A <c>string</c> containing the field value</returns>
        /// <remarks>When the data is searched, the value for the field will be looked up first using the field&apos;s
        /// &quot;alias&quot; (<c>aliasName</c>), then using the default name (<c>fieldName</c>) if no alias is present.</remarks>
        public static string GetRuleData(this XDocument ruleDocument, string className, string fieldName, int rowId = 1, string defaultValue = null)
        {
            return GetRuleDataElement(ruleDocument, className, fieldName, rowId)?.Value ?? defaultValue;
        }

        /// <summary>
        /// Returns the value for a given field from the contents of an <see cref="XDocument"/>
        /// created from the <see cref="P21.Extensions.BusinessRule.Rule.XmlData"/> as an <see cref="XElement"/>.
        /// </summary>
        /// <param name="ruleDocument">An <see cref="XDocument"/> representing the Business Rule XML data</param>
        /// <param name="className">The &quot;class name&quot; (table name) where the data field is located</param>
        /// <param name="fieldName">The column name containing the value to return (see Remarks)</param>
        /// <param name="rowId">The <c>rowID</c> of the value (defaults to <c>1</c>)</param>
        /// <returns>A <c>string</c> containing the field value</returns>
        /// <remarks>When the data is searched, the value for the field will be looked up first using the field&apos;s
        /// &quot;alias&quot; (<c>aliasName</c>), then using the default name (<c>fieldName</c>) if no alias is present.</remarks>
        public static XElement GetRuleDataElement(this XDocument ruleDocument, string className, string fieldName, int rowId = 1)
        {
            try
            {
                return ruleDocument.XPathSelectElement(
                           $"//fieldList[className='{className}' and fieldAlias='{fieldName}' and rowID='{rowId}']/fieldValue") ??
                       ruleDocument.XPathSelectElement(
                           $"//fieldList[className='{className}' and fieldName='{fieldName}' and rowID='{rowId}']/fieldValue");            }
            catch
            {
                // Ignored
            }

            return null;
        }
    }
    #pragma warning restore CA1031
}
