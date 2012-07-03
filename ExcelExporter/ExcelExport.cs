﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;

namespace ExcelExporter {
	///<summary>Exports a collection of tables to an Excel spreadsheet.</summary>
	public class ExcelExport {
		readonly List<IExcelSheet> sheets = new List<IExcelSheet>();

		///<summary>Adds a collection of strongly-typed objects to be exported.</summary>
		///<param name="name">The name of the sheet to generate.</param>
		///<param name="items">The rows to export to the sheet.</param>
		///<returns>This instance, to allow chaining.kds</returns>
		public ExcelExport AddSheet<TRow>(string name, IEnumerable<TRow> items) {
			sheets.Add(new TypedSheet<TRow>(name, items));
			return this;
		}

		///<summary>Exports all of the added sheets to an Excel file.</summary>
		///<param name="fileName">The filename to export to.  The file type is inferred from the extension.</param>
		public void ExportTo(string fileName) {
			ExportTo(fileName, GetDBType(Path.GetExtension(fileName)));
		}
		///<summary>Exports all of the added sheets to an Excel file.</summary>
		public void ExportTo(string fileName, ExcelFormat format) {
			using (var connection = new OleDbConnection(GetConnectionString(fileName, format))) {
				connection.Open();
				foreach (var sheet in sheets) {
					sheet.Export(connection);
				}
			}
		}

		#region Excel Formats
		static readonly List<KeyValuePair<ExcelFormat, string>> FormatExtensions = new List<KeyValuePair<ExcelFormat, string>> {
			new KeyValuePair<ExcelFormat, string>(ExcelFormat.Excel2003,			".xls"),
			new KeyValuePair<ExcelFormat, string>(ExcelFormat.Excel2007,			".xlsx"),
			new KeyValuePair<ExcelFormat, string>(ExcelFormat.Excel2007Binary,		".xlsb"),
			new KeyValuePair<ExcelFormat, string>(ExcelFormat.Excel2007Macro,		".xlsm"),
		};

		///<summary>Gets the database format that uses the given extension.</summary>
		public static ExcelFormat GetDBType(string extension) {
			var pair = FormatExtensions.FirstOrDefault(kvp => kvp.Value.Equals(extension, StringComparison.OrdinalIgnoreCase));

			if (pair.Value == null)
				throw new ArgumentException("Unrecognized extension: " + extension, "extension");
			return pair.Key;
		}

		///<summary>Gets the file extension for a database format.</summary>
		public static string GetExtension(ExcelFormat format) { return FormatExtensions.First(kvp => kvp.Key == format).Value; }

		static string GetConnectionString(string filePath) { return GetConnectionString(filePath, GetDBType(Path.GetExtension(filePath))); }
		static string GetConnectionString(string filePath, ExcelFormat format) {
			if (String.IsNullOrEmpty(filePath)) throw new ArgumentNullException("filePath");

			var csBuilder = new OleDbConnectionStringBuilder { DataSource = filePath, PersistSecurityInfo = false };

			const string ExcelProperties = "IMEX=0;HDR=YES";
			switch (format) {
				case ExcelFormat.Excel2003:
					csBuilder.Provider = "Microsoft.Jet.OLEDB.4.0";
					csBuilder["Extended Properties"] = "Excel 8.0;" + ExcelProperties;
					break;
				case ExcelFormat.Excel2007:
					csBuilder.Provider = "Microsoft.ACE.OLEDB.12.0";
					csBuilder["Extended Properties"] = "Excel 12.0 Xml;" + ExcelProperties;
					break;
				case ExcelFormat.Excel2007Binary:
					csBuilder.Provider = "Microsoft.ACE.OLEDB.12.0";
					csBuilder["Extended Properties"] = "Excel 12.0;" + ExcelProperties;
					break;
				case ExcelFormat.Excel2007Macro:
					csBuilder.Provider = "Microsoft.ACE.OLEDB.12.0";
					csBuilder["Extended Properties"] = "Excel 12.0 Macro;" + ExcelProperties;
					break;
			}

			return csBuilder.ToString();
		}
		#endregion
	}

	///<summary>A format for a database file.</summary>
	public enum ExcelFormat {
		///<summary>An Excel 97-2003 .xls file.</summary>
		Excel2003,
		///<summary>An Excel 2007 .xlsx file.</summary>
		Excel2007,
		///<summary>An Excel 2007 .xlsb binary file.</summary>
		Excel2007Binary,
		///<summary>An Excel 2007 .xlsm file with macros.</summary>
		Excel2007Macro
	}

	interface IExcelSheet {
		void Export(IDbConnection connection);
	}
}