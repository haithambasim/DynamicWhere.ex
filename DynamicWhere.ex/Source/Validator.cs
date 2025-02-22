﻿using System.Reflection;

namespace DynamicWhere.ex;

/// <summary>
/// Provides validation methods for various entities used in dynamic queries.
/// </summary>
internal static class Validator
{
    /// <summary>
    /// Validates a <see cref="Condition"/> instance to ensure it meets the specified criteria.
    /// </summary>
    /// <typeparam name="T">The type in which the condition is applied.</typeparam>
    /// <param name="condition">The <see cref="Condition"/> instance to validate.</param>
    /// <exception cref="LogicException">
    /// Thrown when the condition or its components are found to be invalid based on the specified criteria.
    /// </exception>
    public static void Validate<T>(this Condition condition)
    {
        // Check if the field name is provided and not empty.
        if (string.IsNullOrWhiteSpace(condition.Field))
        {
            throw new LogicException(ErrorCode.InvalidField);
        }

        // Validate the field name against the specified type.
        condition.Field.Validate<T>();

        // Handle validation based on the operator type.
        switch (condition.Operator)
        {
            case Operator.Between:
            case Operator.NotBetween:
                // For Between and NotBetween operators, exactly two values are required.
                if (condition.Values.Count != 2)
                {
                    throw new LogicException(ErrorCode.RequiredTwoValue);
                }
                break;

            case Operator.In:
            case Operator.NotIn:
            case Operator.IIn:
            case Operator.INotIn:
                // For In, NotIn, IIn, and INotIn operators, at least one value is required
                if (condition.Values.Count == 0)
                {
                    throw new LogicException(ErrorCode.RequiredValues);
                }
                break;

            case Operator.IsNull:
            case Operator.IsNotNull:
                // For IsNull and IsNotNull operators, values are not required.
                if (condition.Values.Count != 0)
                {
                    throw new LogicException(ErrorCode.NotRequiredValues);
                }
                break;

            default:
                // For other operators, exactly one value is required.
                if (condition.Values.Count != 1)
                {
                    throw new LogicException(ErrorCode.RequiredOneValue(condition.Operator.ToString()));
                }
                break;
        }

        switch (condition.DataType)
        {
            case DataType.Guid:
                // For GUID fields, the value must be a valid GUID format.
                if (condition.Values.Any(value => !Guid.TryParse(value, out _)))
                {
                    throw new LogicException(ErrorCode.InvalidFormat);
                }
                break;

            case DataType.Text:
                // For text fields, the value must not be null or whitespace.
                if (condition.Values.Any(string.IsNullOrWhiteSpace))
                {
                    throw new LogicException(ErrorCode.InvalidFormat);
                }
                break;

            case DataType.Number:
                // For number fields, the value must be a valid number format.
                if (condition.Values.Any(value => 
                    !byte.TryParse(value, out _) &&
                    !short.TryParse(value, out _) &&
                    !int.TryParse(value, out _) &&
                    !long.TryParse(value, out _) &&
                    !float.TryParse(value, out _) &&
                    !double.TryParse(value, out _) &&
                    !decimal.TryParse(value, out _)))
                {
                    throw new LogicException(ErrorCode.InvalidFormat);
                }
                break;

            case DataType.Boolean:
                // For boolean fields, the value must be a valid boolean format.
                if (condition.Values.Any(value => !bool.TryParse(value, out _)))
                {
                    throw new LogicException(ErrorCode.InvalidFormat);
                }
                break;

            case DataType.Date:
            case DataType.DateTime:
                // For date fields, the value must be a valid date format.
                if (condition.Values.Any(value => !DateTime.TryParse(value, out _)))
                {
                    throw new LogicException(ErrorCode.InvalidFormat);
                }
                break;
        }
    }

    /// <summary>
    /// Validates a <see cref="ConditionGroup"/> instance to ensure it meets the specified criteria.
    /// </summary>
    /// <param name="group">The <see cref="ConditionGroup"/> instance to validate.</param>
    /// <exception cref="LogicException">
    /// Thrown when the condition group or its components are found to have duplicate sorting values based on the specified criteria.
    /// </exception>
    public static void Validate(this ConditionGroup group)
    {
        // Check for duplicate sorting values among conditions within the group.
        bool hasDuplicateSort = group.Conditions.GroupBy(x => x.Sort).Any(x => x.Count() > 1);

        if (hasDuplicateSort)
        {
            throw new LogicException(ErrorCode.ConditionsUniqueSort);
        }

        // Check for duplicate sorting values among subcondition groups within the group.
        hasDuplicateSort = group.SubConditionGroups.GroupBy(x => x.Sort).Any(x => x.Count() > 1);

        if (hasDuplicateSort)
        {
            throw new LogicException(ErrorCode.SubConditionsGroupsUniqueSort);
        }
    }

    /// <summary>
    /// Validates an <see cref="OrderBy"/> instance to ensure it has a valid field name.
    /// </summary>
    /// <typeparam name="T">The type to validate the field name against.</typeparam>
    /// <param name="order">The <see cref="OrderBy"/> instance to validate.</param>
    /// <exception cref="LogicException">Thrown if the field name is invalid or empty.</exception>
    public static void Validate<T>(this OrderBy order)
    {
        // Check if the field name is provided and not empty.
        if (string.IsNullOrWhiteSpace(order.Field))
        {
            throw new LogicException(ErrorCode.InvalidField);
        }

        // Validate the field name against the specified type.
        order.Field.Validate<T>();
    }

    /// <summary>
    /// Validates the parameters of a paging request to ensure they are within acceptable limits.
    /// </summary>
    /// <typeparam name="T">The type of paging configuration (usually a DTO or model).</typeparam>
    /// <param name="page">The paging configuration to validate.</param>
    /// <exception cref="LogicException">Thrown when the page number or page size is invalid.</exception>
    public static void Validate<T>(this PageBy page)
    {
        // Check page number if it a positive integer.
        if (page.PageNumber < 0)
        {
            throw new LogicException(ErrorCode.InvalidPageNumber);
        }

        // Check page size if it is a positive integer.
        if (page.PageSize < 0)
        {
            throw new LogicException(ErrorCode.InvalidPageSize);
        }
    }

    /// <summary>
    /// Validates the condition sets within a <see cref="Segment"/> and retrieves them if they meet the specified criteria.
    /// </summary>
    /// <param name="segment">The <see cref="Segment"/> instance containing the condition sets to validate.</param>
    /// <returns>
    /// A list of valid condition sets within the segment.
    /// </returns>
    /// <exception cref="LogicException">
    /// Thrown when the segment or its condition sets do not meet the specified criteria.
    /// </exception>
    public static List<ConditionSet> ValidateAndGetSets(this Segment segment)
    {
        if (segment.ConditionSets.Count == 0)
        {
            return new List<ConditionSet>();
        }

        // Check for duplicate sorting values among condition sets.
        bool hasDuplicateSort = segment.ConditionSets.GroupBy(x => x.Sort).Any(x => x.Count() > 1);

        if (hasDuplicateSort)
        {
            throw new LogicException(ErrorCode.SetsUniqueSort);
        }

        // Check if any condition set except the first one has a null intersection.
        bool hasNullIntersection = segment.ConditionSets.OrderBy(x => x.Sort).Skip(1).Any(x => x.Intersection == null);

        if (hasNullIntersection)
        {
            throw new LogicException(ErrorCode.RequiredIntersection);
        }

        // sort the condition sets by their sorting values.
        List<ConditionSet> sets = segment.ConditionSets.OrderBy(x => x.Sort).ToList();

        // Set the intersection of the first condition set to null to represent the absence of intersection.
        sets[0].Intersection = null;

        return sets;
    }

    /// <summary>
    /// Validates a string representing a property name within a nested object hierarchy for a given type.
    /// </summary>
    /// <typeparam name="T">The type in which the property name is validated.</typeparam>
    /// <param name="name">The property name to validate, possibly containing nested property names separated by dots.</param>
    /// <exception cref="LogicException">Thrown when the property name is invalid or not found within the type's hierarchy.</exception>
    public static void Validate<T>(this string name)
    {
        // Split the property name by dots if it contains any, handling leading/trailing whitespaces and empty entries.
        List<string> names = name.Contains('.')
            ? name.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : new List<string> { name };

        // Ensure that at least one valid property name is provided.
        if (names.Count == 0)
        {
            throw new LogicException(ErrorCode.InvalidField);
        }

        Type type = typeof(T);

        // Iterate through nested property names and validate each level.
        foreach (string propertyName in names)
        {
            // Attempt to retrieve the PropertyInfo for the current property name.
            PropertyInfo? prop = type.GetProperty(propertyName);

            // If the property is not found, throw an exception indicating an invalid field.
            if (prop == null)
            {
                throw new LogicException(ErrorCode.InvalidField);
            }

            // Update the current type to the property's type, considering generic List<> types.
            type = prop.PropertyType;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                type = type.GetGenericArguments()[0];
            }
        }
    }
}
