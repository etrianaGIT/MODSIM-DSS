using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace ASquared.SymbolicMath
{
	public enum OrientationOption
	{
		RowVector,
		ColumnVector
	}

	/// <summary>Defines matrices.</summary>
	public class SymbolicMatrix
	{
		#region Instance variables

		public DataTable m_table;

		#endregion

		#region Properties

		/// <summary>
		/// Gets and sets the underlying Data Table for this matrix.
		/// </summary>
		public DataTable TabularValues
		{
			get
			{
				return m_table;
			}

			set
			{
				m_table = value;
			}
		}

		/// <summary>
		/// Gets the 2D array representation of this matrix.
		/// </summary>
		public Symbol[,] Values
		{
			get
			{
				return ToArray();
			}
		}

		/// <summary>
		/// Gets the number of rows this matrix contains.
		/// </summary>
		public Int32 NumberOfRows
		{
			get
			{
				return m_table.Rows.Count;
			}
		}

		/// <summary>
		/// Gets the number of columns this matrix contains
		/// </summary>
		public Int32 NumberOfColumns
		{
			get
			{
				return m_table.Columns.Count;
			}
		}

		/// <summary>
		/// Gets the total number of elements this matrix contains.
		/// </summary>
		public Int32 NumberOfElements
		{
			get
			{
				return NumberOfRows * NumberOfColumns;
			}
		}

		/// <summary>
		/// Gets the total number of elements this matrix contains.
		/// </summary>
		public Int32 Length
		{
			get
			{
				return NumberOfElements;
			}
		}

		#endregion

		#region Constructors

		/// <summary>Constructs an instance of Matrix.</summary>
		/// <param name="numRows">The number of rows in the matrix or vector.</param>
        [CLSCompliant(false)]
        public SymbolicMatrix(Int32 numRows)
			: this(numRows, (Symbol)0)
		{
		}

		/// <summary>Constructs an instance of Matrix.</summary>
		/// <param name="numRows">The number of rows in the matrix or vector.</param>
		/// <param name="defaultValue">The default value within all entries of the array.</param>
        [CLSCompliant(false)]
        public SymbolicMatrix(Int32 numRows, Symbol defaultValue)
			: this(numRows, 1, defaultValue)
		{
		}

		/// <summary>Constructs an instance of Matrix.</summary>
		/// <param name="numRows">The number of rows in the matrix or vector.</param>
		/// <param name="numCols">The number of columns in the matrix.</param>
        [CLSCompliant(false)]
        public SymbolicMatrix(Int32 numRows, Int32 numCols)
			: this(numRows, numCols, (Symbol)0)
		{
		}

		/// <summary>Constructs an instance of Matrix.</summary>
		/// <param name="numRows">The number of rows in the matrix or vector.</param>
		/// <param name="numCols">The number of columns in the matrix.</param>
		/// <param name="defaultValue">The default value within all entries of the array.</param>
        [CLSCompliant(false)]
        public SymbolicMatrix(Int32 numRows, Int32 numCols, Symbol defaultValue)
			: this(BuildTable(numRows, numCols, defaultValue))
		{
		}

		/// <summary>Constructs an instance of Matrix.</summary>
		/// <param name="values">The array defining the values within the vector.</param>
        [CLSCompliant(false)]
        public SymbolicMatrix(Symbol[] values)
			: this(ConvertArrayToDataTable(values, OrientationOption.ColumnVector))
		{
		}

		/// <summary>Constructs an instance of Matrix.</summary>
		/// <param name="values">The array defining the values within the vector.</param>
		/// <param name="orientation">The orientation of the new vector.</param>
        [CLSCompliant(false)]
        public SymbolicMatrix(Symbol[] values, OrientationOption orientation)
			: this(ConvertArrayToDataTable(values, orientation))
		{
		}

		/// <summary>Constructs an instance of Matrix.</summary>
		/// <param name="values">The array defining the values within the matrix.</param>
        [CLSCompliant(false)]
        public SymbolicMatrix(Symbol[,] values)
			: this(ConvertArrayToDataTable(values))
		{
		}

		/// <summary>Constructs an instance of Matrix.</summary>
		/// <param name="table">The datatable defining the values within the matrix.</param>
        [CLSCompliant(false)]
        public SymbolicMatrix(DataTable table)
		{
			m_table = table;
		}

		#endregion

		#region Indexing

		/// <summary>Gets and sets an indexed value from a vector.</summary>
		/// <param name="index">The index at which to locate the value.</param>
		/// <remarks>If this instance is a matrix (not a vector), an indexed value from the first column will be returned if possible.</remarks>
		public Symbol this[Int32 index]
		{
			get
			{
				if (NumberOfRows == 0 || NumberOfColumns == 0)
					return 0;

				if (NumberOfRows == 1 && index < NumberOfColumns)
					return (Symbol)m_table.Rows[0][index];
				else if (index < NumberOfRows)
					return (Symbol)m_table.Rows[index][0];

				return default(Symbol);
			}
			set
			{
				if (NumberOfRows == 0 || NumberOfColumns == 0)
					return;
				
				if (NumberOfRows == 1 && index < NumberOfColumns)
					m_table.Rows[0][index] = value;
				else if (index < NumberOfRows)
					m_table.Rows[index][0] = value;
			}
		}

		/// <summary>Gets and sets an indexed value from a matrix.</summary>
		/// <param name="row">The row index.</param>
		/// <param name="col">The column index.</param>
		public Symbol this[Int32 row, Int32 col]
		{
			get
			{
				if (NumberOfRows == 0 || NumberOfColumns == 0)
					return 0;

				if (row < NumberOfRows && col < NumberOfColumns)
					return (Symbol)m_table.Rows[row][col];

				return 0;
			}
			set
			{
				if (NumberOfRows == 0 || NumberOfColumns == 0)
					return;

				if (-1 < row && row < NumberOfRows && -1 < col && col < NumberOfColumns)
					m_table.Rows[row][col] = value;
				else
					throw new Exception("Row " + row + " and column " + col + " are not valid indices within this matrix.");
			}
		}

		#endregion

		#region Data structure methods

		/// <summary>
		/// Returns a deep copy of this matrix.
		/// </summary>
		public SymbolicMatrix Copy()
		{
			return new SymbolicMatrix(m_table.Copy());
		}

		// Constructing the table
		/// <summary>Converts this instance to a 2D array of Symbols.</summary>
		public Symbol[,] ToArray()
		{
			Int32 nrows = NumberOfRows;
			Int32 ncols = NumberOfColumns;
			Symbol[,] retVal = new Symbol[nrows, ncols];

			for (Int32 i = 0; i < nrows; i++)
				for (Int32 j = 0; j < ncols; j++)
					retVal[i, j] = (Symbol)m_table.Rows[i][j];

			return retVal;
		}

        /// <summary>Converts this instance to a 1-dimensional array of doubles. If there are actually 2-dimensions in the Matrix, this returns the first column.</summary>
        public Symbol[] To1DArray()
        {
            if (this.NumberOfRows == 0)
                return new Symbol[0];
            bool firstRow = (this.NumberOfRows == 1);
            int nvals = firstRow ? this.NumberOfColumns : this.NumberOfRows;
            if (firstRow)
                return this.GetRowArray(0);
            else
                return this.GetColArray(0);
        }

		/// <summary>Converts a 1D array to a 2D array.</summary>
		/// <param name="values">The 1D array.</param>
		/// <param name="orientation">Specifies whether the values represent a row vector or a column vector.</param>
		public static Symbol[,] ConvertArray(Symbol[] values, OrientationOption orientation)
		{
			Symbol[,] newvals;
			if (orientation == OrientationOption.RowVector)
			{
				newvals = new Symbol[1, values.Length];

				for (Int32 i = 0; i < values.Length; i++)
					newvals[0, i] = values[i];
			}
			else
			{
				newvals = new Symbol[values.Length, 1];

				for (Int32 i = 0; i < values.Length; i++)
					newvals[i, 0] = values[i];
			}

			return newvals;
		}

		/// <summary>Converts a 2D array of doubles to a 2D array of Symbols.</summary>
		/// <param name="values">2D array of doubles</param>
		public static Symbol[,] ConvertArray(Double[,] values)
		{
			int nrows = values.GetLength(0); 
			int ncols = values.GetLength(1); 
			Symbol[,] s = new Symbol[nrows, ncols];
			for (int i = 0; i < nrows; i++)
				for (int j = 0; j < ncols; j++)
					s[i, j] = (Symbol)values[i,j];
			return s;
		}

		/// <summary>Converts a 1D array of Symbols into a DataTable.</summary>
		/// <param name="values">The 1D array of values.</param>
		/// <param name="orientation">Specifies whether the values represent a row vector or a column vector.</param>
		public static DataTable ConvertArrayToDataTable(Symbol[] values, OrientationOption orientation)
		{
			return ConvertArrayToDataTable(ConvertArray(values, orientation));
		}
		
		/// <summary>Converts a 2D array of Symbols into a DataTable.</summary>
		/// <param name="values">The 2D array of values.</param>
		public static DataTable ConvertArrayToDataTable(Symbol[,] values)
		{
			if (values == null || values.Length == 0) return null;

			// Set up the table
			DataTable dt = new DataTable();
			Int32 nrows = values.GetLength(0);
			Int32 ncols = values.GetLength(1);

			// Add all the columns (type Symbol)
			for (Int32 j = 0; j < ncols; j++)
				dt.Columns.Add(j.ToString(), typeof(Symbol));

			// Add all the rows and associated values
			for (Int32 i = 0; i < nrows; i++)
			{
				DataRow dr = dt.NewRow();
				for (Int32 j = 0; j < ncols; j++) dr[j] = values[i, j];
				dt.Rows.Add(dr);
			}

			return dt;
		}
		
		/// <summary>Builds a table with a specified number of rows and columns and default values.</summary>
		/// <param name="numRows">The number of rows in the datatable.</param>
		/// <param name="numCols">The number of columns in the datatable.</param>
		/// <param name="defaultValue">The default value for every element in the datatable.</param>
		public static DataTable BuildTable(Int32 numRows, Int32 numCols, Symbol defaultValue)
		{
			Symbol[,] vals = new Symbol[numRows, numCols];

			for (Int32 i = 0; i < numRows; i++)
				for (Int32 j = 0; j < numCols; j++)
					vals[i, j] = defaultValue;

			return ConvertArrayToDataTable(vals);
		}
		
		/// <summary>Converts a 2D array of Symbols into a Matrix.</summary>
		/// <param name="values">The 2D array of values.</param>
		public static SymbolicMatrix FromArray(Symbol[,] values)
		{
			return new SymbolicMatrix(ConvertArrayToDataTable(values));
		}

		// Adding on to the table 
		/// <summary>Inserts the rows of the given matrix into this matrix at a given position.</summary>
		/// <param name="newRows">A matrix of values with the same number of columns as this matrix.</param>
		/// <param name="pos">The position for the first new row.</param>
		public void InsertRowsAt(SymbolicMatrix newRows, Int32 pos)
		{
			if (newRows.NumberOfColumns != NumberOfColumns)
				throw new ArgumentException("newRows needs to have the same amount of columns as the matrix.");
			
			for (Int32 i = newRows.NumberOfRows - 1; i >= 0; i--)
			{
				DataRow dr = m_table.NewRow();
				Symbol[] row = newRows.GetRowArray(i);

				for (Int32 j = 0; j < row.Length; j++)
					dr[j] = row[j];
				
				m_table.Rows.InsertAt(dr, pos);
			}
		}

		/// <summary>Inserts the columns of the given matrix into this matrix at a given position.</summary>
		/// <param name="newCols">A matrix of values with the same number of rows as this matrix.</param>
		/// <param name="pos">The position for the first new column.</param>
		public void InsertColsAt(SymbolicMatrix newCols, Int32 pos)
		{
			if (newCols.NumberOfRows != NumberOfRows)
				throw new ArgumentException("newCols needs to have the same amount of elements as the number of rows in the matrix.");
			
			Int32 nrows = newCols.NumberOfRows;
			Int32 ncols = newCols.NumberOfColumns;

			for (Int32 j = ncols - 1; j >= 0; j--)
			{
				Int32 oldpos = NumberOfColumns;
				m_table.Columns.Add(oldpos.ToString(), typeof(Symbol));

				if (oldpos != pos)
					m_table.Columns[oldpos].SetOrdinal(pos);
				
				Symbol[] col = newCols.GetColArray(j);
				
				for (Int32 i = 0; i < nrows; i++)
					m_table.Rows[i][pos] = col[i];
			}
		}

		/// <summary>Appends a matrix as new rows to this matrix.</summary>
		/// <param name="newRows">A matrix of values with the same number of columns as this matrix.</param>
		public void AppendRows(SymbolicMatrix newRows)
		{
			InsertRowsAt(newRows, NumberOfRows);
		}

		/// <summary>Appends rows to the matrix.</summary>
		/// <param name="numRows">The number of rows to add.</param>
		public void AppendRows(Int32 numRows)
		{
			for (Int32 i = 0; i < numRows; i++)
			{
				DataRow dr = m_table.NewRow();

				for (Int32 j = 0; j < m_table.Columns.Count; j++)
					dr[j] = (Symbol)0;

				m_table.Rows.Add(dr);
			}
		}

		/// <summary>Appends a matrix as new columns to this matrix.</summary>
		/// <param name="newCols">A matrix of values with the same number of rows as this matrix.</param>
		public void AppendCols(SymbolicMatrix newCols)
		{
			InsertColsAt(newCols, NumberOfColumns);
		}

		/// <summary>Adds columns to the matrix.</summary>
		/// <param name="numCols">The number of columns to add.</param>
		public void AppendCols(Int32 numCols)
		{
			for (Int32 i = 0; i < numCols; i++)
			{
				m_table.Columns.Add(m_table.Columns.Count.ToString(), typeof(Symbol));

				for (Int32 row = 0; row < m_table.Rows.Count; row++)
					m_table.Rows[row][m_table.Columns.Count - 1] = (Symbol)0;
			}
		}

		// Removing from the table
		/// <summary>Removes a row at the specified position.</summary>
		/// <param name="pos">The index at which to remove the row.</param>
		public SymbolicMatrix RemoveRowAt(Int32 pos)
		{
			SymbolicMatrix row = this.GetRow(pos);

			m_table.Rows.RemoveAt(pos);
			
			return row;
		}

		/// <summary>Removes a column at the specified position.</summary>
		/// <param name="pos">The index at which to remove the column.</param>
		public SymbolicMatrix RemoveColAt(Int32 pos)
		{
			SymbolicMatrix col = this.GetCol(pos);

			m_table.Columns.RemoveAt(pos);
			
			return col;
		}

		// Moving and replacing
		/// <summary>Moves a row from its original position to another.</summary>
		/// <param name="fromPos">The position of the row to move.</param>
		/// <param name="toPos">The position to which to move the row.</param>
		public void MoveRow(Int32 fromPos, Int32 toPos)
		{
			if (fromPos == toPos)
				return;
			
			SymbolicMatrix row = this.RemoveRowAt(fromPos);
			
			InsertRowsAt(row, toPos);
		}

		/// <summary>Moves a column from its original position to another.</summary>
		/// <param name="fromPos">The position of the column to move.</param>
		/// <param name="toPos">The position to which to move the column.</param>
		public void MoveCol(Int32 fromPos, Int32 toPos)
		{
			if (fromPos == toPos)
				return;
			
			m_table.Columns[fromPos].SetOrdinal(toPos);
		}

		/// <summary>Replaces the row in the specified position with the specified matrix.</summary>
		/// <param name="values">The matrix to replace the row.</param>
		/// <param name="pos">The position of the row.</param>
		public void ReplaceRow(Int32 pos, SymbolicMatrix values)
		{
			RemoveRowAt(pos);
			InsertRowsAt(values, pos);
		}
		
		/// <summary>Replaces the column in the specified position with the specified matrix.</summary>
		/// <param name="values">The matrix to replace the column.</param>
		/// <param name="pos">The position of the column.</param>
		public void ReplaceCol(Int32 pos, SymbolicMatrix values)
		{
			RemoveColAt(pos);
			InsertColsAt(values, pos);
		}

		// Getting rows and columns
		/// <summary>Gets a row of the Matrix.</summary>
		/// <param name="pos">The zero-based index of the row.</param>
		/// <returns>Return a 1 x ncols Matrix with the values from the specified row.</returns>
		public SymbolicMatrix GetRow(Int32 pos)
		{
			return new SymbolicMatrix(this.GetRowArray(pos), OrientationOption.RowVector);
		}

		/// <summary>Gets a column of the Matrix.</summary>
		/// <param name="pos">The zero-based index of the column.</param>
		/// <returns>Return a nrows x 1 Matrix with the values from the specified column.</returns>
		public SymbolicMatrix GetCol(Int32 pos)
		{
			return new SymbolicMatrix(this.GetColArray(pos));
		}

		/// <summary>Gets rows from this instance.</summary>
		/// <param name="pos">An array of zero-based indices of the rows to fetch.</param>
		/// <returns>Return a pos.Length x ncols Matrix with the values from the specified row.</returns>
		public SymbolicMatrix GetRows(Int32[] pos)
		{
			if (pos.Length == 0) return null;
			SymbolicMatrix m = this.GetRow(pos[0]);
			for (Int32 i = 1; i < pos.Length; i++)
				m.AppendRows(this.GetRow(pos[i]));
			return m;
		}

		/// <summary>Gets columns from this instance.</summary>
		/// <param name="pos">An array of zero-based indices of the columns to fetch.</param>
		/// <returns>Return a nrows x pos.Length Matrix with the values from the specified column.</returns>
		public SymbolicMatrix GetCols(Int32[] pos)
		{
			if (pos.Length == 0) return null;
			SymbolicMatrix m = this.GetCol(pos[0]);
			for (Int32 i = 1; i < pos.Length; i++)
				m.AppendCols(this.GetCol(pos[i]));
			return m;
		}

		/// <summary>Gets rows from this instance.</summary>
		/// <param name="fromPos">The index of the first row to get.</param>
		/// <param name="toPos">The index of the last row to get.</param>
		/// <returns>Return a pos.Length x ncols Matrix with the values from the specified row.</returns>
		public SymbolicMatrix GetRows(Int32 fromPos, Int32 toPos)
		{
			if (fromPos > toPos || fromPos >= NumberOfRows) return null;
			toPos = Math.Min(toPos, NumberOfRows - 1);
			SymbolicMatrix m = this.GetRow(fromPos);
			for (Int32 i = fromPos + 1; i <= toPos; i++)
				m.AppendRows(this.GetRow(i));
			return m;
		}

		/// <summary>Gets columns from this instance.</summary>
		/// <param name="fromPos">The index of the first column to get.</param>
		/// <param name="toPos">The index of the last column to get.</param>
		/// <returns>Return a nrows x pos.Length Matrix with the values from the specified column.</returns>
		public SymbolicMatrix GetCols(Int32 fromPos, Int32 toPos)
		{
			if (fromPos > toPos || fromPos >= NumberOfColumns) return null;
			toPos = Math.Min(toPos, NumberOfColumns - 1);
			SymbolicMatrix m = this.GetCol(fromPos);
			for (Int32 i = fromPos + 1; i <= toPos; i++)
				m.AppendCols(this.GetCol(i));
			return m;
		}

		/// <summary>Gets a row of values from the Matrix.</summary>
		/// <param name="pos">The zero-based index of the row.</param>
		public Symbol[] GetRowArray(Int32 pos)
		{
			Symbol[] vals = new Symbol[NumberOfColumns];
			for (Int32 i = 0; i < NumberOfColumns; i++)
				vals[i] = (Symbol)m_table.Rows[pos][i];
			return vals;
		}

		/// <summary>Gets a column of the Matrix.</summary>
		/// <param name="pos">The zero-based index of the column.</param>
		public Symbol[] GetColArray(Int32 pos)
		{
			Symbol[] vals = new Symbol[NumberOfRows];
			for (Int32 i = 0; i < NumberOfRows; i++)
				vals[i] = (Symbol)m_table.Rows[i][pos];
			return vals;
		}

		#endregion

		#region Matrix math methods

		/// <summary>Add a matrix to this matrix</summary>
		/// <param name="m">The matrix to add.</param>
		/// <returns>Returns the resulting matrix.</returns>
		public SymbolicMatrix Add(SymbolicMatrix m)
		{
			if (NumberOfRows != m.NumberOfRows || NumberOfColumns != m.NumberOfColumns)
				throw new Exception("Cannot add two matrices of different sizes.");

			Int32 nrows = NumberOfRows;
			Int32 ncols = NumberOfColumns;
			Symbol[,] vals = new Symbol[nrows, ncols];
			for (Int32 i = 0; i < nrows; i++)
				for (Int32 j = 0; j < ncols; j++)
					vals[i, j] = (Symbol)m_table.Rows[i][j] + (Symbol)m.m_table.Rows[i][j];
			//Symbol[,] newvals = Convert.
			return new SymbolicMatrix(vals);
		}

		/// <summary>Add a matrix to this matrix</summary>
		/// <param name="m">The matrix to add.</param>
		/// <returns>Returns the resulting matrix.</returns>
		public SymbolicMatrix Add(Symbol val)
		{
			Int32 nrows = NumberOfRows;
			Int32 ncols = NumberOfColumns;
			Symbol[,] vals = new Symbol[nrows, ncols];
			for (Int32 i = 0; i < nrows; i++)
				for (Int32 j = 0; j < ncols; j++)
					vals[i, j] = (Symbol)m_table.Rows[i][j] + val;
			return new SymbolicMatrix(vals);
		}
		
		/// <summary>Multiply matrices against each other or against a scalar value.</summary>
		/// <param name="scalar">The scalar value.</param>
		/// <returns>Returns the resulting matrix.</returns>
		public SymbolicMatrix Mult(Symbol scalar)
		{
			Int32 nrows = NumberOfRows;
			Int32 ncols = NumberOfColumns;
			Symbol[,] vals = new Symbol[nrows, ncols];
			for (Int32 i = 0; i < nrows; i++)
				for (Int32 j = 0; j < ncols; j++)
					vals[i, j] = (Symbol)scalar * (Symbol)m_table.Rows[i][j];
			return new SymbolicMatrix(vals);
		}

		/// <summary>Multiply matrices against each other or against a scalar value.</summary>
		/// <param name="m">The matrix to multiply.</param>
		public SymbolicMatrix Mult(SymbolicMatrix m, TransposeOption tOpt)
		{
			bool transposeNeeded = false;
			if (NumberOfColumns != m.NumberOfRows)
			{
				if ((tOpt == TransposeOption.EitherAsNeeded || tOpt == TransposeOption.LeftAsNeeded) && NumberOfRows == m.NumberOfRows)
				{
					transposeNeeded = true;
					tOpt = TransposeOption.LeftAsNeeded;
				}
				else if ((tOpt == TransposeOption.EitherAsNeeded || tOpt == TransposeOption.RightAsNeeded) && NumberOfColumns == m.NumberOfColumns)
				{
					transposeNeeded = true;
					tOpt = TransposeOption.RightAsNeeded;
				}
				else
				{
					throw new Exception("Cannot multiply two matrices of different sizes.");
				}
			}

			Symbol[,] vals;
			Symbol val;
			if (transposeNeeded)
			{
				if (tOpt == TransposeOption.LeftAsNeeded)
				{
					vals = new Symbol[NumberOfColumns, m.NumberOfColumns];
					for (Int32 thiscol = 0; thiscol < NumberOfColumns; thiscol++)
					{
						for (Int32 mcol = 0; mcol < m.NumberOfColumns; mcol++)
						{
							val = 0;
							for (Int32 i = 0; i < NumberOfRows; i++)
								val += (Symbol)m_table.Rows[i][thiscol] * (Symbol)m.m_table.Rows[i][mcol];
							vals[thiscol, mcol] = val;
						}
					}
				}
				else // implies tOpt == TransposeAsNeeded.m_matrix
				{
					vals = new Symbol[NumberOfRows, m.NumberOfColumns];
					for (Int32 thisrow = 0; thisrow < NumberOfRows; thisrow++)
					{
						for (Int32 mrow = 0; mrow < m.NumberOfRows; mrow++)
						{
							val = 0;
							for (Int32 i = 0; i < NumberOfColumns; i++)
								val += (Symbol)m_table.Rows[thisrow][i] * (Symbol)m.m_table.Rows[mrow][i];
							vals[thisrow, mrow] = val;
						}
					}
				}
			}
			else
			{
				vals = new Symbol[NumberOfRows, m.NumberOfColumns];
				for (Int32 thisrow = 0; thisrow < NumberOfRows; thisrow++)
				{
					for (Int32 mcol = 0; mcol < m.NumberOfColumns; mcol++)
					{
						val = 0;
						for (Int32 i = 0; i < NumberOfColumns; i++)
							val += (Symbol)m_table.Rows[thisrow][i] * (Symbol)m.m_table.Rows[i][mcol];
						vals[thisrow, mcol] = val;
					}
				}
			}
			return new SymbolicMatrix(vals);
		}
		
		/// <summary>Multiply each corresponding element in this matrix with those in m.</summary>
		/// <param name="m">The matrix with which to perform piecewise multiplication.</param>
		public SymbolicMatrix ElementwiseMult(SymbolicMatrix m)
		{
			if (NumberOfRows != m.NumberOfRows || NumberOfColumns != m.NumberOfColumns)
				throw new ArgumentException("m must have the same number of elements as this instance.");
			Int32 nrows = NumberOfRows;
			Int32 ncols = NumberOfColumns;
			SymbolicMatrix newmat = this.Copy();
			for (Int32 i = 0; i < NumberOfRows; i++)
				for (Int32 j = 0; j < NumberOfColumns; j++)
					newmat.m_table.Rows[i][j] = (Symbol)m_table.Rows[i][j] * (Symbol)m.m_table.Rows[i][j];
			return newmat;
		}
		
		/// <summary>Divide each corresponding element in this matrix with those in m.</summary>
		/// <param name="m">The matrix with which to perform piecewise division.</param>
		public SymbolicMatrix ElementwiseDivide(SymbolicMatrix m)
		{
			if (NumberOfRows != m.NumberOfRows || NumberOfColumns != m.NumberOfColumns)
				throw new ArgumentException("m must have the same number of elements as this instance.");
			Int32 nrows = NumberOfRows;
			Int32 ncols = NumberOfColumns;
			SymbolicMatrix newmat = this.Copy();
			for (Int32 i = 0; i < NumberOfRows; i++)
				for (Int32 j = 0; j < NumberOfColumns; j++)
					newmat.m_table.Rows[i][j] = (Symbol)m_table.Rows[i][j] / (Symbol)m.m_table.Rows[i][j];
			return newmat;
		}
		
		/// <summary>Raises the matrix to a power of 'b'.</summary>
		/// <param name="b">The power to raise the matrix to.</param>
		/// <returns>Returns the resulting matrix.</returns>
		public SymbolicMatrix Pow(Int32 b)
		{
			if (b == 1)
				return this.Copy();
			else if (b > 1)
				return this * this.Pow(b - 1);
			else if (b == 0)
				return SymbolicMatrix.Identity(NumberOfRows);
			else //(b < 0)
				return this.Inverse().Pow(-b);
		}
		
		///// <summary>Takes the absolute value of every element.</summary>
		///// <returns>Returns the resulting matrix.</returns>
		//public SymbolicMatrix Abs()
		//{
		//    Symbol[,] vals = new Symbol[NumberOfRows, NumberOfColumns];
		//    for (Int32 i = 0; i < NumberOfRows; i++)
		//        for (Int32 j = 0; j < NumberOfColumns; j++)
		//            vals[i, j] = Math.Abs((Symbol)_table.Rows[i][j]);
		//    return new SymbolicMatrix(vals);
		//}
		
		/// <summary>Gets the transpose of this instance.</summary>
		/// <returns>Returns the transpose of this instance.</returns>
		public SymbolicMatrix Transpose()
		{
			Symbol[,] vals = new Symbol[NumberOfColumns, NumberOfRows];
			for (Int32 i = 0; i < NumberOfRows; i++)
				for (Int32 j = 0; j < NumberOfColumns; j++)
					vals[j, i] = (Symbol)m_table.Rows[i][j];
			return new SymbolicMatrix(vals);
		}

		// Taking inverse
		/// <summary>Finds the next row with a non-zero value.</summary>
		/// <param name="startingRow">The starting row.</param>
		/// <param name="col">The column to search in.</param>
		/// <returns>Returns the index of the row with a non-zero value. Returns -1 if not found.</returns>
		private Int32 NextNonZeroRow(Int32 startingRow, Int32 col)
		{
			for (Int32 i = startingRow; i < NumberOfRows; i++)
				if (((Symbol)m_table.Rows[i][col]).ToNumber() != 0)
					return i;
			return -1;
		}

		/// <summary>Sets the next row with a non-zero value to the specified ordinate.</summary>
		/// <param name="ordinate">Specifies the index to which the next row with a non-zero value will be placed.</param>
		/// <param name="col">Specifies the column to search.</param>
		private void SetNonZeroRowOrdinate(Int32 ordinate, Int32 col)
		{
			Int32 oldrow = this.NextNonZeroRow(ordinate, col);
			if (oldrow == -1) throw new Exception("Inverse does not exist for the following matrix:\n" + this.ToString());
			this.MoveRow(oldrow, ordinate);
		}

		/// <summary>Pivots the whole matrix about the value at the specified row and column.</summary>
		/// <param name="row">The row of the pivot.</param>
		/// <param name="col">The column of the pivot.</param>
		public void Pivot(Int32 row, Int32 col)
		{
			this.ReplaceRow(row, this.GetRow(row) / (Symbol)m_table.Rows[row][col]);
			for (Int32 i = 0; i < NumberOfRows; i++)
				if (i != row)
					this.ReplaceRow(i, this.GetRow(i) - this.GetRow(row) * (Symbol)m_table.Rows[i][col]);
		}
		
		/// <summary>Gets the reduced row echelon form of the matrix.</summary>
		/// <returns>Returns the matrix in its reduced row echelon form.</returns>
		public void RREF()
		{
			Int32 ncols = Math.Min(NumberOfRows, NumberOfColumns);
			for (Int32 col = 0; col < ncols; col++)
			{
				this.SetNonZeroRowOrdinate(col, col);
				this.Pivot(col, col);
			}
		}
		
		/// <summary>Gets the inverse of this instance.</summary>
		/// <returns>Returns the inverse of this instance.</returns>
		public SymbolicMatrix Inverse()
		{
			if (NumberOfRows != NumberOfColumns) throw new Exception("Must be a square matrix to invert.");
			SymbolicMatrix newmat = this.Copy();
			newmat.AppendCols(SymbolicMatrix.Identity(NumberOfRows));
			newmat.RREF();
			return newmat.GetCols(NumberOfRows, newmat.NumberOfColumns - 1);
		}
		
		/// <summary>Solves for 'x' in Ax = b.</summary>
		/// <param name="b">The righthand side of the Ax = b equation.</param>
		/// <returns>Returns the Matrix for x.</returns>
		public static SymbolicMatrix SolveLinearSys(SymbolicMatrix A, SymbolicMatrix b)
		{
			if (A.NumberOfRows != A.NumberOfColumns)
				throw new Exception("A needs to be a square matrix to have a unique solution.");
			if (b.NumberOfRows != A.NumberOfRows)
				throw new ArgumentException("b needs to have the same number of rows as A.");
			SymbolicMatrix newmat = A.Copy();
			newmat.AppendCols(b);
			newmat.RREF();
			return newmat.GetCols(A.NumberOfRows, newmat.NumberOfColumns - 1);
		}

		#endregion

		#region Overrides

		/// <summary>Writes the string of the matrix.</summary>
		/// <returns>Returns the string of the matrix.</returns>
		public override string ToString()
		{
			if (NumberOfRows == 0 || NumberOfColumns == 0) return "[]";
			StringBuilder s = new StringBuilder();
			for (Int32 i = 0; i < NumberOfRows; i++)
			{
				s.Append(m_table.Rows[i][0].ToString());
				for (Int32 j = 1; j < NumberOfColumns; j++)
					s.Append(" | " + ((Symbol)m_table.Rows[i][j]).ToString());
				s.Append("\n");
			}
			return s.ToString(0, s.Length - 1);
		}
		
		/// <summary>Gets a matrix from a string (similar to the way MATLAB does it).</summary>
		/// <param name="matrix">The string representing a matrix.</param>
		/// <returns>Returns the Matrix represented in the string.</returns>
		public static SymbolicMatrix FromString(string matrix)
		{
			char[] brackets = new char[] { '[', ']' };
			char[] rowDividers = new char[] { ';', '\n' };
			char[] colDividers = new char[] { '|' };
			matrix = matrix.Trim(brackets);
			if (matrix == "") return null;

			// Split via rows
			string[] rows = matrix.Split(rowDividers, StringSplitOptions.RemoveEmptyEntries);
			Int32 nrows = rows.Length;
			if (nrows == 0) return null;

			// Split via columns
			string[] cols = rows[0].Split(colDividers, StringSplitOptions.RemoveEmptyEntries);
			Int32 ncols = cols.Length;
			if (ncols == 0) return null;

			// Fill the values
			Symbol[,] vals = new Symbol[nrows, ncols];
			for (Int32 i = 0; i < nrows; i++)
			{
				cols = rows[i].Split(colDividers, StringSplitOptions.RemoveEmptyEntries);
				for (Int32 j = 0; j < ncols; j++)
					vals[i, j] = Symbol.Parse(cols[j]);
			}
			return new SymbolicMatrix(vals);
		}

		#endregion

		#region Operators

		// arrays
        [CLSCompliant(false)]
		public static implicit operator SymbolicMatrix(Matrix m)
		{
			return new SymbolicMatrix(ConvertArray(m.ToArray()));
		}
        [CLSCompliant(false)]
		public static implicit operator SymbolicMatrix(Symbol[] v)
		{
			return new SymbolicMatrix(v);
		}
        [CLSCompliant(false)]
		public static implicit operator SymbolicMatrix(Symbol[,] a)
		{
			return new SymbolicMatrix(a);
		}
        [CLSCompliant(false)]
		public static implicit operator Symbol[](SymbolicMatrix m)
		{
			if (m == (SymbolicMatrix)null || m.NumberOfColumns == 0 || m.NumberOfRows == 0) return null;
			if (m.NumberOfRows == 1)
				return m.GetRowArray(0);
			else
				return m.GetColArray(0);
		}
        [CLSCompliant(false)]
		public static implicit operator Symbol[,](SymbolicMatrix m)
		{
			if (m == (SymbolicMatrix)null) return null;
			return m.ToArray();
		}

		// strings 
		public static implicit operator string(SymbolicMatrix m)
		{
			return m.ToString();
		}
		public static implicit operator SymbolicMatrix(string s)
		{
			return SymbolicMatrix.FromString(s);
		}
		public static string operator +(SymbolicMatrix m, string s)
		{
			if (m == (SymbolicMatrix)null) return s;
			return m.ToString() + s;
		}
		public static string operator +(string s, SymbolicMatrix m)
		{
			if (m == (SymbolicMatrix)null) return s;
			return s + m.ToString();
		}

		// math
		public static SymbolicMatrix operator *(SymbolicMatrix a, SymbolicMatrix b)
		{
			return a.Mult(b, TransposeOption.None);
		}
		public static SymbolicMatrix operator *(SymbolicMatrix a, Symbol b)
		{
			return a.Mult(b);
		}
		public static SymbolicMatrix operator *(Symbol b, SymbolicMatrix a)
		{
			return a.Mult(b);
		}
		public static SymbolicMatrix operator +(SymbolicMatrix a, SymbolicMatrix b)
		{
			return a.Add(b);
		}
		public static SymbolicMatrix operator +(SymbolicMatrix a, Symbol b)
		{
			return a.Add(b);
		}
		public static SymbolicMatrix operator +(Symbol b, SymbolicMatrix a)
		{
			return a.Add(b);
		}
		public static SymbolicMatrix operator -(SymbolicMatrix a, SymbolicMatrix b)
		{
			return a.Add(b.Mult(-1));
		}
		public static SymbolicMatrix operator -(SymbolicMatrix a, Symbol b)
		{
			return a.Add(-b);
		}
		public static SymbolicMatrix operator -(Symbol b, SymbolicMatrix a)
		{
			return a.Mult(-1).Add(b);
		}
		public static SymbolicMatrix operator /(SymbolicMatrix a, Symbol b)
		{
			return a.Mult(1 / b);
		}

		#endregion

		#region Built-in matrices

		/// <summary>Builds the identity matrix.</summary>
		/// <param name="n">The number of rows and columns in the identity matrix.</param>
		/// <returns>Returns the identity matrix.</returns>
		public static SymbolicMatrix Identity(Int32 n)
		{
			SymbolicMatrix I = new SymbolicMatrix(n, n, 0);
			for (Int32 i = 0; i < I.NumberOfRows; i++)
				I.m_table.Rows[i][i] = 1;
			return I;
		}

		#endregion
	}
}
