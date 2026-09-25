# SQLiteHelper UpdateTables Optimization Reference

**Purpose:** Potential refactor for improved performance in bulk INSERT/UPDATE/DELETE operations using Microsoft.Data.Sqlite

---

## Overview

This document contains an optimized version of the `UpdateTables()` method and supporting helper functions from `ModsimMain\NetworkUtils\SQLiteHelper.cs`.

**Key Improvement:** Prepare SQL commands and parameter structures once per table, then reuse them by only updating parameter values for each row. This eliminates redundant command object creation, SQL string parsing, and parameter setup.

**Expected Performance Gain:** 2-5x speedup for typical tables; 10x+ for insert-heavy operations.

---

## Optimized Code

```csharp
public double UpdateTables(DataSet m_DSet)
{
	int maxAttempts = 100;
	Stopwatch watch = new Stopwatch();
	watch.Start();
	workingOnUpdates = true;
	int totRows = 0;
	CheckDatabaseConnection();

	foreach (DataTable table in m_DSet.Tables)
	{
		if (table.Rows.Count == 0) continue;

		int triesCount = 0;
		string currentTable = table.TableName;

		// OPTIMIZATION: Pre-analyze table schema once instead of per-row
		var primaryKeys = table.PrimaryKey ?? new DataColumn[0];
		var nonKeyColumns = table.Columns.Cast<DataColumn>()
			.Where(c => !primaryKeys.Contains(c))
			.ToArray();

	reStartUpdate:
		try
		{
			// OPTIMIZATION: Prepare commands once per table, not per row.
			// Parameterized SQL is parsed and compiled by SQLite once, then reused.
			using (var insertCmd = PrepareInsertCommand(table, _sqlconnection, _sqltransaction))
			using (var updateCmd = PrepareUpdateCommand(table, primaryKeys, nonKeyColumns, _sqlconnection, _sqltransaction))
			using (var deleteCmd = PrepareDeleteCommand(table, primaryKeys, _sqlconnection, _sqltransaction))
			{
				DataRow[] rows = table.Rows.Cast<DataRow>().ToArray();

				foreach (DataRow row in rows)
				{
					if (row.RowState == DataRowState.Unchanged || row.RowState == DataRowState.Detached)
						continue;

					int updatedRows = 0;

					// OPTIMIZATION: Only update parameter VALUES for each row, not the entire command.
					if (row.RowState == DataRowState.Added)
					{
						BindInsertParameters(insertCmd, table, row);
						updatedRows = insertCmd.ExecuteNonQuery();
					}
					else if (row.RowState == DataRowState.Modified)
					{
						BindUpdateParameters(updateCmd, row, nonKeyColumns, primaryKeys);
						updatedRows = updateCmd.ExecuteNonQuery();
					}
					else if (row.RowState == DataRowState.Deleted)
					{
						BindDeleteParameters(deleteCmd, row, primaryKeys);
						updatedRows = deleteCmd.ExecuteNonQuery();
					}

					totRows += updatedRows;
					row.AcceptChanges();
				}
			}
		}
		catch (Exception ex)
		{
			if (ex.Message.Contains("locked"))
			{
				if (triesCount == 1) FireErrorMessage(" Background output processing re-opening DB connection.");
				System.Threading.Thread.Sleep(400);
				triesCount++;
				if (triesCount < maxAttempts) goto reStartUpdate;
				FireErrorMessage("[ERROR processing output]: " + Environment.NewLine + ex.Message);
				FireErrorMessage("[ERROR processing output]: Failed to process table: " + currentTable);
			}
			else
				FireErrorMessage(ex.Message);
		}
	}

	workingOnUpdates = false;
	watch.Stop();
	return (totRows / watch.Elapsed.TotalSeconds);
}

// Pre-build the INSERT statement and parameter structure once.
// The actual values will be bound later in BindInsertParameters().
private SqliteCommand PrepareInsertCommand(DataTable table, SqliteConnection conn, SqliteTransaction trans)
{
	var cmd = conn.CreateCommand();
	cmd.Transaction = trans;
	var cols = table.Columns.Cast<DataColumn>().ToList();
	var paramNames = Enumerable.Range(0, cols.Count).Select(i => $"$p{i}").ToList();

	for (int i = 0; i < cols.Count; i++)
		cmd.Parameters.AddWithValue($"$p{i}", DBNull.Value);

	cmd.CommandText = $"INSERT INTO {QuoteIdentifier(table.TableName)} " +
		$"({string.Join(", ", cols.Select(c => QuoteIdentifier(c.ColumnName)))}) " +
		$"VALUES ({string.Join(", ", paramNames)});";

	return cmd;
}

// Pre-build the UPDATE statement once. Parameters will be bound per-row.
private SqliteCommand PrepareUpdateCommand(DataTable table, DataColumn[] primaryKeys, DataColumn[] nonKeyColumns,
	SqliteConnection conn, SqliteTransaction trans)
{
	var cmd = conn.CreateCommand();
	cmd.Transaction = trans;

	for (int i = 0; i < nonKeyColumns.Length; i++)
	{
		cmd.Parameters.AddWithValue($"$value{i}", DBNull.Value);
	}

	for (int i = 0; i < primaryKeys.Length; i++)
	{
		cmd.Parameters.AddWithValue($"$key{i}", DBNull.Value);
	}

	var setClauses = string.Join(", ", Enumerable.Range(0, nonKeyColumns.Length)
		.Select(i => $"{QuoteIdentifier(nonKeyColumns[i].ColumnName)} = $value{i}"));
	var whereClauses = string.Join(" AND ", Enumerable.Range(0, primaryKeys.Length)
		.Select(i => $"{QuoteIdentifier(primaryKeys[i].ColumnName)} = $key{i}"));

	cmd.CommandText = $"UPDATE {QuoteIdentifier(table.TableName)} SET {setClauses} WHERE {whereClauses};";

	return cmd;
}

// Pre-build the DELETE statement once.
private SqliteCommand PrepareDeleteCommand(DataTable table, DataColumn[] primaryKeys,
	SqliteConnection conn, SqliteTransaction trans)
{
	var cmd = conn.CreateCommand();
	cmd.Transaction = trans;

	for (int i = 0; i < primaryKeys.Length; i++)
		cmd.Parameters.AddWithValue($"$key{i}", DBNull.Value);

	var whereClauses = string.Join(" AND ", Enumerable.Range(0, primaryKeys.Length)
		.Select(i => $"{QuoteIdentifier(primaryKeys[i].ColumnName)} = $key{i}"));

	cmd.CommandText = $"DELETE FROM {QuoteIdentifier(table.TableName)} WHERE {whereClauses};";

	return cmd;
}

// For each INSERT row, just update parameter values (no SQL rebuild).
private void BindInsertParameters(SqliteCommand cmd, DataTable table, DataRow row)
{
	for (int i = 0; i < table.Columns.Count; i++)
		cmd.Parameters[$"$p{i}"].Value = row[i, DataRowVersion.Current] ?? DBNull.Value;
}

// For each UPDATE row, bind SET values and WHERE key values.
private void BindUpdateParameters(SqliteCommand cmd, DataRow row, DataColumn[] nonKeyColumns, DataColumn[] primaryKeys)
{
	for (int i = 0; i < nonKeyColumns.Length; i++)
		cmd.Parameters[$"$value{i}"].Value = row[nonKeyColumns[i], DataRowVersion.Current] ?? DBNull.Value;

	for (int i = 0; i < primaryKeys.Length; i++)
		cmd.Parameters[$"$key{i}"].Value = row[primaryKeys[i], DataRowVersion.Original] ?? DBNull.Value;
}

// For each DELETE row, bind the key values to find the row.
private void BindDeleteParameters(SqliteCommand cmd, DataRow row, DataColumn[] primaryKeys)
{
	for (int i = 0; i < primaryKeys.Length; i++)
		cmd.Parameters[$"$key{i}"].Value = row[primaryKeys[i], DataRowVersion.Original] ?? DBNull.Value;
}

private string QuoteIdentifier(string identifier)
{
	return "\"" + identifier.Replace("\"", "\"\"") + "\"";
}
```

---

## Performance Comparison

| Metric | Current Implementation | Optimized Implementation |
|--------|------------------------|--------------------------|
| Command objects created per table | N (one per row) | 3 (INSERT, UPDATE, DELETE) |
| SQL string parsing | N times | 3 times |
| Parameter object allocation | ~N × column count | 3 × column count |
| Total allocations for 1,000-row table | ~1,000+ objects | ~10 objects |
| Expected speedup | Baseline | 2-10x |

---

## Implementation Notes

1. **Backward Compatibility:** This refactor is a drop-in replacement for the current `UpdateTables()` method and supporting helper methods. No changes to method signatures or public API.

2. **Transaction Safety:** Commands continue to use `_sqltransaction`, maintaining the existing transaction model.

3. **Error Handling:** Retry logic for "database locked" errors is preserved.

4. **Multi-Framework Support:** Works with both .NET Framework 4.8 and .NET 8 projects that reference `NetworkUtils.csproj`.

5. **Testing Recommendations:**
   - Verify result row counts match before/after implementation
   - Profile actual timeseries insertion performance on large datasets
   - Test on Linux (Docker) to ensure no platform-specific issues

---

## Alternative Approaches Considered

- **Entity Framework Core:** Not viable due to dual .NET Framework 4.8 + .NET 8 targets requiring two different ORM implementations
- **Nested command preparation:** Less readable; this version balances performance and maintainability
- **Batch INSERT statements:** Possible but adds complexity for mixed operation types (INSERT/UPDATE/DELETE in same table)

---

## References

- Current implementation: `ModsimMain\NetworkUtils\SQLiteHelper.cs` (lines ~759-859)
- Microsoft.Data.Sqlite documentation: https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/
- DataRow state management: https://learn.microsoft.com/en-us/dotnet/api/system.data.datarowstate

---

**Status:** Ready for implementation review. Do not implement without approval and performance benchmarking on production data.
