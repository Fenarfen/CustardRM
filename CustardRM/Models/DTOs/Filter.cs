﻿using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Models.DTOs;

public class Filter
{
	public class Field
    {
        public bool Enabled { get; set; } = false;
    }
    public class FilterField<T> : Field
    {
        public T Value { get; set; }
    }

    public class RangeFilterField<T> : Field
    {
        public T LowerValue { get; set; }
        public T UpperValue { get; set; }
    }

	public int GetEnabledCount()
	{
		int count = 0;
		foreach (var prop in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			var propValue = prop.GetValue(this);
			if (propValue == null)
				continue;

			var enabledProp = prop.PropertyType.GetProperty("Enabled");
			if (enabledProp != null)
			{
				var enabledValue = enabledProp.GetValue(propValue);
				if (enabledValue is bool isEnabled && isEnabled)
				{
					count++;
				}
			}
		}
		return count;
	}

	public class StockItemFilter : Filter
    {
        public FilterField<string> ItemCodeFilter { get; set; } = new();
        public FilterField<string> ItemNameFilter { get; set; } = new();
        public FilterField<string> DescriptionFilter { get; set; } = new();
        public RangeFilterField<decimal?> UnitPriceFilter { get; set; } = new();
        public RangeFilterField<decimal?> CostPriceFilter { get; set; } = new();
        public FilterField<bool> IsActiveFilter { get; set; } = new();
        public RangeFilterField<DateTime?> CreatedAtFilter { get; set; } = new();
        public RangeFilterField<DateTime?> UpdatedAtFilter { get; set; } = new();
    }

	public class CustomerFilter : Filter
	{
		public FilterField<string> UserIDFilter { get; set; } = new();
		public FilterField<string> ProfileNameFilter { get; set; } = new();
		public FilterField<string> EmailFilter { get; set; } = new();
		public RangeFilterField<DateTime?> CreatedAtFilter { get; set; } = new();
	}

	public class SupplierFilter : Filter
	{
		public FilterField<string> NameFilter { get; set; } = new();
		public FilterField<bool> IsActiveFilter { get; set; } = new();
		public RangeFilterField<DateTime?> CreatedAtFilter { get; set; } = new();
	}

	public class SalesOrderFilter : Filter
	{
		public RangeFilterField<DateTime?> OrderDateFilter { get; set; } = new();
		public FilterField<string> CustomerRefFilter { get; set; } = new();
		public RangeFilterField<decimal?> TotalAmountFilter { get; set; } = new();
	}

	public class PurchaseOrderFilter : Filter
	{
		public FilterField<int> SupplierIDFilter { get; set; } = new();
		public RangeFilterField<decimal?> TotalAmount { get; set; } = new();
	}
}
