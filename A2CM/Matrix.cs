using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ASquared
{
    public enum TransposeOption
	{
		EitherAsNeeded,
		LeftAsNeeded,
		RightAsNeeded,
		None
	}

    /// <summary>Defines matrices.</summary>
    public class Matrix
    {
        #region Instance variables

        public DataTable table;

        #endregion

        #region Properties

        public DataTable TabularValues { get { return this.table; } set { this.table = value; } }
        public Double[,] Values { get { return this.ToArray(); } }
        public Int32 NumOfRows { get { return (this.table == null) ? 0 : this.table.Rows.Count; } }
        public Int32 NumOfCols { get { return (this.table == null) ? 0 : this.table.Columns.Count; } }
        public Int32 Length { get { return this.NumOfRows * this.NumOfCols; } }

        #endregion

        #region Constructors

        /// <summary>Constructs an empty instance of Matrix.</summary>
        [CLSCompliant(false)]
        public Matrix()
        {
            this.table = new DataTable(); 
        }
        /// <summary>Constructs an instance of Matrix.</summary>
        /// <param name="nrows">The number of rows in the matrix or vector.</param>
        [CLSCompliant(false)]
        public Matrix(Int32 nrows)
            : this(nrows, default(Double))
        {
        }
        /// <summary>Constructs an instance of Matrix.</summary>
        /// <param name="nrows">The number of rows in the matrix or vector.</param>
        /// <param name="defaultVal">The default value within all entries of the array.</param>
        [CLSCompliant(false)]
        public Matrix(Int32 nrows, Double defaultVal)
            : this(nrows, 1, defaultVal)
        {
        }
        /// <summary>Constructs an instance of Matrix.</summary>
        /// <param name="nrows">The number of rows in the matrix or vector.</param>
        /// <param name="ncols">The number of columns in the matrix.</param>
        [CLSCompliant(false)]
        public Matrix(Int32 nrows, Int32 ncols)
            : this(nrows, ncols, default(Double))
        {
        }
        /// <summary>Constructs an instance of Matrix.</summary>
        /// <param name="nrows">The number of rows in the matrix or vector.</param>
        /// <param name="ncols">The number of columns in the matrix.</param>
        /// <param name="defaultVal">The default value within all entries of the array.</param>
        [CLSCompliant(false)]
        public Matrix(Int32 nrows, Int32 ncols, Double defaultVal)
            : this(BuildTable(nrows, ncols, defaultVal))
        {
        }
        /// <summary>Constructs an instance of Matrix.</summary>
        /// <param name="vals">The values defining the values within the vector.</param>
        [CLSCompliant(false)]
        public Matrix(Double[] vals)
            : this(Matrix.ConvertArrayToDT(vals, false))
        {
        }
        /// <summary>Constructs an instance of Matrix.</summary>
        /// <param name="vals">The values defining the values within the vector.</param>
        [CLSCompliant(false)]
        public Matrix(Double[] vals, bool makesUpRow)
            : this(Matrix.ConvertArrayToDT(vals, makesUpRow))
        {
        }
        /// <summary>Constructs an instance of Matrix.</summary>
        /// <param name="vals">The values defining the values within the matrix.</param>
        [CLSCompliant(false)]
        public Matrix(Double[,] vals)
            : this(Matrix.ConvertArrayToDT(vals))
        {
        }
        /// <summary>Constructs an instance of Matrix.</summary>
        /// <param name="dt">The datatable defining the values within the matrix.</param>
        [CLSCompliant(false)]
        public Matrix(DataTable dt)
        {
            this.table = dt;
        }

        #endregion

        #region Indexing

        /// <summary>Gets and sets an indexed value from a vector.</summary>
        /// <param name="index">The index at which to locate the value.</param>
        /// <remarks>If this instance is a matrix (not a vector), an indexed value from the first column will be returned if possible.</remarks>
        public Double this[Int32 index]
        {
            get
            {
                if (this.NumOfRows == 0 || this.NumOfCols == 0) return default(Double);
                if (this.NumOfRows == 1 && index < this.NumOfCols)
                    return (Double)this.table.Rows[0][index];
                else if (index < this.NumOfRows)
                    return (Double)this.table.Rows[index][0];
                return default(Double);
            }
            set
            {
                if (this.NumOfRows == 0 || this.NumOfCols == 0) return;
                if (this.NumOfRows == 1 && index < this.NumOfCols)
                    this.table.Rows[0][index] = value;
                else if (index < this.NumOfRows)
                    this.table.Rows[index][0] = value;
            }
        }
        /// <summary>Gets and sets an indexed value from a matrix.</summary>
        /// <param name="row">The row index.</param>
        /// <param name="col">The column index.</param>
        public Double this[Int32 row, Int32 col]
        {
            get
            {
                if (this.NumOfRows == 0 || this.NumOfCols == 0) return default(Double);
                if (row < this.NumOfRows && col < this.NumOfCols)
                    return (Double)this.table.Rows[row][col];
                return default(Double); 
            }
            set
            {
                if (this.NumOfRows == 0 || this.NumOfCols == 0) return;
                if (row < this.NumOfRows && col < this.NumOfCols)
                    this.table.Rows[row][col] = value;
            }
        }

        #endregion

        #region Data structure methods

        // Copying the matrix
        public Matrix Copy()
        {
            return new Matrix(this.table.Copy());
        }

        // Constructing the table
        /// <summary>Converts this instance to an array of doubles. If the Matrix is a vector, the returning vector is of size Double[NumOfRows, 1].</summary>
        public Double[,] ToArray()
        {
            Int32 nrows = this.NumOfRows;
            Int32 ncols = this.NumOfCols;
            Double[,] retVal = new Double[nrows, ncols];
            for (Int32 i = 0; i < nrows; i++)
                for (Int32 j = 0; j < ncols; j++)
                    retVal[i, j] = (Double)this.table.Rows[i][j];
            return retVal;
        }
        /// <summary>Converts this instance to a 1-dimensional array of doubles. If there are actually 2-dimensions in the Matrix, this returns the first column.</summary>
        public Double[] To1DArray()
        {
            if (this.NumOfRows == 0)
                return new Double[0]; 
            bool firstRow = (this.NumOfRows == 1);
            int nvals = firstRow ? this.NumOfCols : this.NumOfRows;
            if (firstRow)
                return this.GetRowArray(0);
            else
                return this.GetColArray(0); 
        }
        /// <summary>Converts this instance to a row matrix (if this has more than one column and more than one row, the first row is returned.</summary>
        public Matrix ToRow()
        {
            if (this.NumOfRows == 0)
                return null; 
            if (this.NumOfCols == 1)
                return this.Transpose();
            else
                return this.GetRow(0); 
        }
        /// <summary>Converts this instance to a row matrix (if this has more than one column and more than one row, the first row is returned.</summary>
        public Matrix ToColumn()
        {
            if (this.NumOfRows == 0)
                return null;
            if (this.NumOfRows == 1)
                return this.Transpose();
            else
                return this.GetCol(0);
        }
        /// <summary>Converts a 1D array to a nested array.</summary>
        /// <param name="vals">The 1D array.</param>
        /// <param name="makesUpRow">Specifies whether vals make up a row. If false, vals make up a column.</param>
        /// <returns>If makesUpRow = true, returns Double[1, vals.Length] filled with values from vals. Otherwise, returns Double[vals.Length, 1] filled with values from vals.</returns>
        public static Double[,] ConvertArray(Double[] vals, bool makesUpRow)
        {
            Double[,] newvals;
            if (makesUpRow)
            {
                newvals = new Double[1, vals.Length];
                for (Int32 i = 0; i < vals.Length; i++)
                    newvals[0, i] = vals[i];
            }
            else
            {
                newvals = new Double[vals.Length, 1];
                for (Int32 i = 0; i < vals.Length; i++)
                    newvals[i, 0] = vals[i];
            }
            return newvals;
        }
        /// <summary>Converts a 1D array of doubles into a datatable.</summary>
        /// <param name="vals">The 1D array of values.</param>
        public static DataTable ConvertArrayToDT(Double[] vals, bool makesUpRow)
        {
            return ConvertArrayToDT(ConvertArray(vals, makesUpRow));
        }
        /// <summary>Converts a 2D array of doubles into a datatable.</summary>
        /// <param name="vals">The 2D array of values.</param>
        public static DataTable ConvertArrayToDT(Double[,] vals)
        {
            if (vals == null || vals.Length == 0) return null;

            // Set up the table
            DataTable dt = new DataTable();
            Int32 nrows = vals.GetLength(0);
            Int32 ncols = vals.GetLength(1);

            // Add all the columns (type Double)
            for (Int32 j = 0; j < ncols; j++)
                dt.Columns.Add(j.ToString(), typeof(Double));

            // Add all the rows and associated values
            for (Int32 i = 0; i < nrows; i++)
            {
                DataRow dr = dt.NewRow();
                for (Int32 j = 0; j < ncols; j++) dr[j] = vals[i, j];
                dt.Rows.Add(dr);
            }
            return dt;
        }
        /// <summary>Builds a table with a specified number of rows and columns and default values.</summary>
        /// <param name="nrows">The number of rows in the datatable.</param>
        /// <param name="ncols">The number of columns in the datatable.</param>
        /// <param name="defaultVal">The default value for every element in the datatable.</param>
        public static DataTable BuildTable(Int32 nrows, Int32 ncols, Double defaultVal)
        {
            Double[,] vals = new Double[nrows, ncols];
            for (Int32 i = 0; i < nrows; i++)
                for (Int32 j = 0; j < ncols; j++)
                    vals[i, j] = defaultVal;
            return ConvertArrayToDT(vals);
        }
        /// <summary>Converts a 2D array of doubles into a Matrix.</summary>
        /// <param name="vals">The 2D array of values.</param>
        public static Matrix FromArray(Double[,] vals)
        {
            return new Matrix(ConvertArrayToDT(vals));
        }

        // Adding on to the table 
        /// <summary>Inserts a row into the matrix.</summary>
        /// <param name="vals">A matrix of values with the same columns as this matrix.</param>
        /// <param name="pos">The position for the new row.</param>
        public void InsertRowsAt(Matrix vals, Int32 pos)
        {
            if (this.NumOfCols == 0)
            {
                this.table = vals.table;
                return; 
            }

            if (vals.NumOfCols != this.NumOfCols)
                throw new ArgumentException("vals needs to have the same amount of columns as the matrix.");
            for (Int32 i = vals.NumOfRows - 1; i >= 0; i--)
            {
                DataRow dr = this.table.NewRow();
                Double[] row = vals.GetRowArray(i);
                for (Int32 j = 0; j < row.Length; j++) dr[j] = row[j];
                this.table.Rows.InsertAt(dr, pos);
            }
        }
        /// <summary>Inserts a column into the matrix.</summary>
        /// <param name="vals">An array of values for each row in the column.</param>
        /// <param name="pos">The position for the new column.</param>
        public void InsertColsAt(Matrix vals, Int32 pos)
        {
            if (this.NumOfRows == 0)
            {
                this.table = vals.table;
                return; 
            }

            if (vals.NumOfRows != this.NumOfRows)
                throw new ArgumentException("vals needs to have the same amount of elements as the number of rows in the matrix.");
            Int32 nrows = vals.NumOfRows;
            Int32 ncols = vals.NumOfCols;
            for (Int32 j = ncols - 1; j >= 0; j--)
            {
                Int32 oldpos = this.NumOfCols;
                this.table.Columns.Add(oldpos.ToString(), typeof(Double));
                if (oldpos != pos)
                    this.table.Columns[oldpos].SetOrdinal(pos);
                Double[] col = vals.GetColArray(j);
                for (Int32 i = 0; i < nrows; i++)
                    this.table.Rows[i][pos] = col[i];
            }
        }
        /// <summary>Adds a row to the matrix.</summary>
        /// <param name="vals">An array of values for each column in the row.</param>
        public void AppendRows(Matrix vals)
        {
            this.InsertRowsAt(vals, this.NumOfRows);
        }
        /// <summary>Adds rows to the matrix.</summary>
        /// <param name="nrows">The number of rows to add.</param>
        public void AppendRows(Int32 nrows)
        {
            for (Int32 i = 0; i < nrows; i++)
            {
                DataRow dr = this.table.NewRow();
                for (Int32 j = 0; j < this.table.Columns.Count; j++) dr[j] = 0;
                this.table.Rows.Add(dr);
            }
        }
        /// <summary>Adds a column into the matrix.</summary>
        /// <param name="vals">An array of values for each row in the column.</param>
        public void AppendCols(Matrix vals)
        {
            this.InsertColsAt(vals, this.NumOfCols);
        }
        /// <summary>Adds columns to the matrix.</summary>
        /// <param name="ncols">The number of columns to add.</param>
        public void AppendCols(Int32 ncols)
        {
            for (Int32 i = 0; i < ncols; i++)
            {
                this.table.Columns.Add(this.table.Columns.Count.ToString(), typeof(Double));
                for (Int32 row = 0; row < this.table.Rows.Count; row++)
                    this.table.Rows[row][this.table.Columns.Count - 1] = 0.0; 
            }
        }

        // Removing from the table
        /// <summary>Removes a row at the specified position.</summary>
        /// <param name="pos">The index at which to remove the row.</param>
        public Matrix RemoveRowAt(Int32 pos)
        {
            Matrix row = this.GetRow(pos);
            this.table.Rows.RemoveAt(pos);
            return row;
        }
        /// <summary>Removes a column at the specified position.</summary>
        /// <param name="pos">The index at which to remove the column.</param>
        public Matrix RemoveColAt(Int32 pos)
        {
            Matrix col = this.GetCol(pos);
            this.table.Columns.RemoveAt(pos);
            return col;
        }

        // Moving and replacing
        /// <summary>Moves a row from its original position to another.</summary>
        /// <param name="fromPos">The position of the row to move.</param>
        /// <param name="toPos">The position to which to move the row.</param>
        public void MoveRow(Int32 fromPos, Int32 toPos)
        {
            if (fromPos == toPos) return;
            Matrix row = this.RemoveRowAt(fromPos);
            this.InsertRowsAt(row, toPos);
        }
        /// <summary>Moves a column from its original position to another.</summary>
        /// <param name="fromPos">The position of the column to move.</param>
        /// <param name="toPos">The position to which to move the column.</param>
        public void MoveCol(Int32 fromPos, Int32 toPos)
        {
            if (fromPos == toPos) return;
            this.table.Columns[fromPos].SetOrdinal(toPos);
        }
        /// <summary>Replaces the row in the specified position with the specified matrix.</summary>
        /// <param name="vals">The matrix to replace the row.</param>
        /// <param name="pos">The position of the row.</param>
        public void ReplaceRow(Int32 pos, Matrix vals)
        {
            this.RemoveRowAt(pos);
            this.InsertRowsAt(vals, pos);
        }
        /// <summary>Replaces the column in the specified position with the specified matrix.</summary>
        /// <param name="vals">The matrix to replace the column.</param>
        /// <param name="pos">The position of the column.</param>
        public void ReplaceCol(Int32 pos, Matrix vals)
        {
            this.RemoveColAt(pos);
            this.InsertColsAt(vals, pos);
        }

        // Getting rows and columns
        /// <summary>Gets a row of the Matrix.</summary>
        /// <param name="pos">The zero-based index of the row.</param>
        /// <returns>Return a 1 x ncols Matrix with the values from the specified row.</returns>
        public Matrix GetRow(Int32 pos)
        {
            return new Matrix(this.GetRowArray(pos), true);
        }
        /// <summary>Gets a column of the Matrix.</summary>
        /// <param name="pos">The zero-based index of the column.</param>
        /// <returns>Return a nrows x 1 Matrix with the values from the specified column.</returns>
        public Matrix GetCol(Int32 pos)
        {
            return new Matrix(this.GetColArray(pos));
        }
        /// <summary>Gets rows from this instance.</summary>
        /// <param name="pos">An array of zero-based indices of the rows to fetch.</param>
        /// <returns>Return a pos.Length x ncols Matrix with the values from the specified row.</returns>
        public Matrix GetRows(Int32[] pos)
        {
            if (pos.Length == 0) return null;
            Matrix m = this.GetRow(pos[0]);
            for (Int32 i = 1; i < pos.Length; i++)
                m.AppendRows(this.GetRow(pos[i]));
            return m;
        }

        /// <summary>Gets columns from this instance.</summary>
        /// <param name="pos">An array of zero-based indices of the columns to fetch.</param>
        /// <returns>Return a nrows x pos.Length Matrix with the values from the specified column.</returns>
        public Matrix GetCols(Int32[] pos)
        {
            if (pos.Length == 0) return null;
            Matrix m = this.GetCol(pos[0]);
            for (Int32 i = 1; i < pos.Length; i++)
                m.AppendCols(this.GetCol(pos[i]));
            return m;
        }
        /// <summary>Gets rows from this instance.</summary>
        /// <param name="fromPos">The index of the first row to get.</param>
        /// <param name="toPos">The index of the last row to get.</param>
        /// <returns>Return a pos.Length x ncols Matrix with the values from the specified row.</returns>
        public Matrix GetRows(Int32 fromPos, Int32 toPos)
        {
            if (fromPos > toPos || fromPos >= this.NumOfRows) return null;
            toPos = Math.Min(toPos, this.NumOfRows - 1);
            Matrix m = this.GetRow(fromPos);
            for (Int32 i = fromPos + 1; i <= toPos; i++)
                m.AppendRows(this.GetRow(i));
            return m;
        }
        /// <summary>Gets columns from this instance.</summary>
        /// <param name="fromPos">The index of the first column to get.</param>
        /// <param name="toPos">The index of the last column to get.</param>
        /// <returns>Return a nrows x pos.Length Matrix with the values from the specified column.</returns>
        public Matrix GetCols(Int32 fromPos, Int32 toPos)
        {
            if (fromPos > toPos || fromPos >= this.NumOfCols) return null;
            toPos = Math.Min(toPos, this.NumOfCols - 1);
            Matrix m = this.GetCol(fromPos);
            for (Int32 i = fromPos + 1; i <= toPos; i++)
                m.AppendCols(this.GetCol(i));
            return m;
        }
        /// <summary>Gets a row of values from the Matrix.</summary>
        /// <param name="pos">The zero-based index of the row.</param>
        public Double[] GetRowArray(Int32 pos)
        {
            Double[] vals = new Double[this.NumOfCols];
            for (Int32 i = 0; i < this.NumOfCols; i++)
                vals[i] = (Double)this.table.Rows[pos][i];
            return vals;
        }
        /// <summary>Gets a column of the Matrix.</summary>
        /// <param name="pos">The zero-based index of the column.</param>
        public Double[] GetColArray(Int32 pos)
        {
            Double[] vals = new Double[this.NumOfRows];
            for (Int32 i = 0; i < this.NumOfRows; i++)
                vals[i] = (Double)this.table.Rows[i][pos];
            return vals;
        }

        #endregion
        
		#region Matrix math methods

        /// <summary>Finds the maximum value within the matrix and its indexers.</summary>
        /// <param name="val">The maximum value within the matrix</param>
        public Double Max()
        {
            Int32 row, col;
            return this.Max(out row, out col);
        }
        /// <summary>Finds the maximum value within the matrix and its indexers.</summary>
        /// <param name="val">The maximum value within the matrix</param>
        /// <param name="row">The row where the maximum value was found. -1 if no value was found</param>
        /// <param name="col">The column where the maximum value was found. -1 if no value was found</param>
        public Double Max(out Int32 row, out Int32 col)
        {
            int[] a, b;
            return this.Max(out row, out col, out a, out b); 
        }
        /// <summary>Finds the maximum value within the matrix and its indexers.</summary>
        /// <param name="val">The maximum value within the matrix</param>
        /// <param name="row">The row where the maximum value was found. -1 if no value was found</param>
        /// <param name="col">The column where the maximum value was found. -1 if no value was found</param>
        /// <param name="multVals">Specifies whether the maximum value occurs multiple times.</param>
        public Double Max(out Int32 row, out Int32 col, out Int32[] rowI, out Int32[] colI)
        {
            Double val = Double.NegativeInfinity;
            row = -1;
            col = -1;
            for (Int32 i = 0; i < this.table.Rows.Count; i++)
                for (Int32 j = 0; j < this.table.Columns.Count; j++)
                    if (val < (Double)this.table.Rows[i][j])
                    {
                        val = (Double)this.table.Rows[i][j];
                        row = i;
                        col = j;
                    }
            List<Int32> rows = new List<Int32>();
            List<Int32> cols = new List<Int32>();
            for (Int32 i = 0; i < this.table.Rows.Count; i++)
                for (Int32 j = 0; j < this.table.Columns.Count; j++)
                    if (val == (Double)this.table.Rows[i][j])
                    {
                        rows.Add(i);
                        cols.Add(j);
                    }
            rowI = rows.ToArray();
            colI = cols.ToArray();
            return val; 
        }
        /// <summary>Finds the minimum value within the matrix and its indexers.</summary>
        /// <param name="val">The minimum value within the matrix</param>
        public Double Min()
        {
            Int32 row, col;
            return this.Min(out row, out col); 
        }       
        /// <summary>Finds the minimum value within the matrix and its indexers.</summary>
        /// <param name="val">The minimum value within the matrix</param>
        /// <param name="row">The row where the minimum value was found. -1 if no value was found</param>
        /// <param name="col">The column where the minimum value was found. -1 if no value was found</param>
        public Double Min(out Int32 row, out Int32 col)
        {
            int[] a, b; 
            return this.Min(out row, out col, out a, out b); 
        }
        /// <summary>Finds the minimum value within the matrix and its indexers.</summary>
        /// <param name="val">The minimum value within the matrix</param>
        /// <param name="row">The row where the minimum value was found. -1 if no value was found</param>
        /// <param name="col">The column where the minimum value was found. -1 if no value was found</param>
        /// <param name="multVals">Specifies whether the minimum value occurs multiple times.</param>
        public Double Min(out Int32 row, out Int32 col, out Int32[] rowI, out Int32[] colI)
        {
            Double val = Double.PositiveInfinity;
            row = -1;
            col = -1;
            for (Int32 i = 0; i < this.table.Rows.Count; i++)
                for (Int32 j = 0; j < this.table.Columns.Count; j++)
                    if (val > (Double)this.table.Rows[i][j])
                    {
                        val = (Double)this.table.Rows[i][j];
                        row = i;
                        col = j; 
                    }
            List<Int32> rows = new List<Int32>();
            List<Int32> cols = new List<Int32>(); 
            for (Int32 i = 0; i < this.table.Rows.Count; i++)
                for (Int32 j = 0; j < this.table.Columns.Count; j++)
                    if (val == (Double)this.table.Rows[i][j])
                    {
                        rows.Add(i);
                        cols.Add(j);
                    }
            rowI = rows.ToArray();
            colI = cols.ToArray();
            return val; 
        }
        public Double Sum()
        {
            Double sum = 0.0;
            for (int i = 0; i < this.table.Rows.Count; i++)
                for (int j = 0; j < this.table.Columns.Count; j++)
                    sum += (Double)this.table.Rows[i][j];
            return sum;
        }
        public Double Mean()
        {
            return Sum() / this.Length;
        }
        public Double SumAbs()
        {
            Double sum = 0.0;
            for (int i = 0; i < this.table.Rows.Count; i++)
                for (int j = 0; j < this.table.Columns.Count; j++)
                    sum += Math.Abs((Double)this.table.Rows[i][j]);
            return sum;
        }
        public Double MeanAbs()
        {
            return SumAbs() / this.Length;
        }
        public Boolean IsNan()
        {
            for (int i = 0; i < this.NumOfRows; i++)
                for (int j = 0; j < this.NumOfCols; j++)
                    if (!Double.IsNaN((Double)this.table.Rows[i][j]))
                        return false;
            return true;
        }

        /// <summary>Add a matrix to this matrix</summary>
        /// <param name="m">The matrix to add.</param>
        /// <returns>Returns the resulting matrix.</returns>
        public Matrix Add(Matrix m)
        {
            if (this.NumOfRows != m.NumOfRows || this.NumOfCols != m.NumOfCols)
                throw new Exception("Cannot add two matrices of different sizes.");

            Int32 nrows = this.NumOfRows;
            Int32 ncols = this.NumOfCols;
            Double[,] vals = new Double[nrows, ncols];
            for (Int32 i = 0; i < nrows; i++)
                for (Int32 j = 0; j < ncols; j++)
                    vals[i, j] = (Double)this.table.Rows[i][j] + (Double)m.table.Rows[i][j];
            return new Matrix(vals);
        }
        /// <summary>Add a matrix to this matrix</summary>
        /// <param name="m">The matrix to add.</param>
        /// <returns>Returns the resulting matrix.</returns>
        public Matrix Add(Double val)
        {
            Int32 nrows = this.NumOfRows;
            Int32 ncols = this.NumOfCols;
            Double[,] vals = new Double[nrows, ncols];
            for (Int32 i = 0; i < nrows; i++)
                for (Int32 j = 0; j < ncols; j++)
                    vals[i, j] = (Double)this.table.Rows[i][j] + val;
            return new Matrix(vals);
        }
        /// <summary>Multiply matrices against each other or against a scalar value.</summary>
        /// <param name="scalar">The scalar value.</param>
        /// <returns>Returns the resulting matrix.</returns>
        public Matrix Mult(Double scalar)
        {
            Int32 nrows = this.NumOfRows;
            Int32 ncols = this.NumOfCols;
            Double[,] vals = new Double[nrows, ncols];
            for (Int32 i = 0; i < nrows; i++)
                for (Int32 j = 0; j < ncols; j++)
                    vals[i, j] = (Double)scalar * (Double)this.table.Rows[i][j];
            return new Matrix(vals);
        }
        /// <summary>Multiply matrices against each other or against a scalar value.</summary>
        /// <param name="m">The matrix to multiply.</param>
        public Matrix Mult(Matrix m, TransposeOption tOpt)
        {
            bool transposeNeeded = false;
            if (this.NumOfCols != m.NumOfRows)
            {
                if ((tOpt == TransposeOption.EitherAsNeeded || tOpt == TransposeOption.LeftAsNeeded) && this.NumOfRows == m.NumOfRows)
                {
                    transposeNeeded = true;
                    tOpt = TransposeOption.LeftAsNeeded;
                }
                else if ((tOpt == TransposeOption.EitherAsNeeded || tOpt == TransposeOption.RightAsNeeded) && this.NumOfCols == m.NumOfCols)
                {
                    transposeNeeded = true;
                    tOpt = TransposeOption.RightAsNeeded;
                }
                else
                {
                    throw new Exception("Cannot multiply two matrices of different sizes.");
                }
            }

            Double[,] vals;
            Double val;
            if (transposeNeeded)
            {
                if (tOpt == TransposeOption.LeftAsNeeded)
                {
                    vals = new Double[this.NumOfCols, m.NumOfCols];
                    for (Int32 thiscol = 0; thiscol < this.NumOfCols; thiscol++)
                    {
                        for (Int32 mcol = 0; mcol < m.NumOfCols; mcol++)
                        {
                            val = 0;
                            for (Int32 i = 0; i < this.NumOfRows; i++)
                                val += (Double)this.table.Rows[i][thiscol] * (Double)m.table.Rows[i][mcol];
                            vals[thiscol, mcol] = val;
                        }
                    }
                }
                else // implies tOpt == TransposeAsNeeded.m_matrix
                {
                    vals = new Double[this.NumOfRows, m.NumOfCols];
                    for (Int32 thisrow = 0; thisrow < this.NumOfRows; thisrow++)
                    {
                        for (Int32 mrow = 0; mrow < m.NumOfRows; mrow++)
                        {
                            val = 0;
                            for (Int32 i = 0; i < this.NumOfCols; i++)
                                val += (Double)this.table.Rows[thisrow][i] * (Double)m.table.Rows[mrow][i];
                            vals[thisrow, mrow] = val;
                        }
                    }
                }
            }
            else
            {
                vals = new Double[this.NumOfRows, m.NumOfCols];
                for (Int32 thisrow = 0; thisrow < this.NumOfRows; thisrow++)
                {
                    for (Int32 mcol = 0; mcol < m.NumOfCols; mcol++)
                    {
                        val = 0;
                        for (Int32 i = 0; i < this.NumOfCols; i++)
                            val += (Double)this.table.Rows[thisrow][i] * (Double)m.table.Rows[i][mcol];
                        vals[thisrow, mcol] = val;
                    }
                }
            }
            return new Matrix(vals);
        }
        /// <summary>Multiply each corresponding element in this matrix with those in m.</summary>
        /// <param name="m">The matrix with which to perform piecewise multiplication.</param>
        public Matrix ElementwiseMult(Matrix m)
        {
            Boolean transpose = false;
            if (this.NumOfRows != m.NumOfRows || this.NumOfCols != m.NumOfCols)
            {
                if (this.NumOfRows == m.NumOfCols && this.NumOfCols == m.NumOfRows)
                    transpose = true; 
                else 
                    throw new ArgumentException("m must have the same number of elements as this instance.");
            }
            Int32 nrows = this.NumOfRows;
            Int32 ncols = this.NumOfCols;
            Matrix newmat = this.Copy();
            if (transpose)
            {
                for (Int32 i = 0; i < this.NumOfRows; i++)
                    for (Int32 j = 0; j < this.NumOfCols; j++)
                        newmat.table.Rows[i][j] = (Double)this.table.Rows[i][j] * (Double)m.table.Rows[j][i];
            }
            else
            {
                for (Int32 i = 0; i < this.NumOfRows; i++)
                    for (Int32 j = 0; j < this.NumOfCols; j++)
                        newmat.table.Rows[i][j] = (Double)this.table.Rows[i][j] * (Double)m.table.Rows[i][j];
            }
            return newmat;
        }
        /// <summary>Divide each corresponding element in this matrix with those in m.</summary>
        /// <param name="m">The matrix with which to perform piecewise division.</param>
        public Matrix ElementwiseDivide(Matrix m)
        {
            Boolean transpose = false;
            if (this.NumOfRows != m.NumOfRows || this.NumOfCols != m.NumOfCols)
            {
                if (this.NumOfRows == m.NumOfCols && this.NumOfCols == m.NumOfRows)
                    transpose = true;
                else
                    throw new ArgumentException("m must have the same number of elements as this instance.");
            }
            Int32 nrows = this.NumOfRows;
            Int32 ncols = this.NumOfCols; 
            Matrix newmat = this.Copy();
            if (transpose)
            {
                for (Int32 i = 0; i < this.NumOfRows; i++)
                    for (Int32 j = 0; j < this.NumOfCols; j++)
                        newmat.table.Rows[i][j] = (Double)this.table.Rows[i][j] / (Double)m.table.Rows[j][i];
            }
            else
            {
                for (Int32 i = 0; i < this.NumOfRows; i++)
                    for (Int32 j = 0; j < this.NumOfCols; j++)
                        newmat.table.Rows[i][j] = (Double)this.table.Rows[i][j] / (Double)m.table.Rows[i][j];
            }
            return newmat; 
        }
        /// <summary>Raises the matrix to a power of 'b'.</summary>
        /// <param name="b">The power to raise the matrix to.</param>
        /// <returns>Returns the resulting matrix.</returns>
        public Matrix Pow(Int32 b)
        {
            if (b == 1)
                return this; 
            else if (b > 1)
                return this * this.Pow(b - 1);
            else if (b == 0)
                return Matrix.Identity(this.NumOfRows);
            else //(b < 0)
                return this.Inverse().Pow(-b);
        }
        /// <summary>Takes the absolute value of every element.</summary>
        /// <returns>Returns the resulting matrix.</returns>
        public Matrix Abs()
        {
            Double[,] vals = new Double[this.NumOfRows, this.NumOfCols];
            for (Int32 i = 0; i < this.NumOfRows; i++)
                for (Int32 j = 0; j < this.NumOfCols; j++)
                    vals[i, j] = Math.Abs((Double)this.table.Rows[i][j]);
            return new Matrix(vals); 
        }
        /// <summary>Gets the transpose of this instance.</summary>
        /// <returns>Returns the transpose of this instance.</returns>
        public Matrix Transpose()
        {
            Double[,] vals = new Double[this.NumOfCols, this.NumOfRows];
            for (Int32 i = 0; i < this.NumOfRows; i++)
                for (Int32 j = 0; j < this.NumOfCols; j++)
                    vals[j, i] = (Double)this.table.Rows[i][j];
            return new Matrix(vals);
        }
        /// <summary>Gets the Euclidean norm or magnitude of this vector (square root of the sum of squared elements, which is performed on matrices as well within this routine)</summary>
        public Double Magnitude()
        {
            Double sum = 0.0;
            for (Int32 i = 0; i < this.NumOfRows; i++)
                for (Int32 j = 0; j < this.NumOfCols; j++)
                    sum += Math.Pow(Convert.ToDouble(this.table.Rows[i][j]), 2);
            return Math.Sqrt(sum); 
        }

        // Taking inverse
        /// <summary>Finds the next row with a non-zero value.</summary>
        /// <param name="startingRow">The starting row.</param>
        /// <param name="col">The column to search in.</param>
        /// <returns>Returns the index of the row with a non-zero value. Returns -1 if not found.</returns>
        private Int32 NextNonZeroRow(Int32 startingRow, Int32 col)
        {
            for (Int32 i = startingRow; i < this.NumOfRows; i++)
                if (Convert.ToDouble(this.table.Rows[i][col]) != 0)
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
            this.ReplaceRow(row, this.GetRow(row) / (Double)this.table.Rows[row][col]);
            for (Int32 i = 0; i < this.NumOfRows; i++)
                if (i != row)
                    this.ReplaceRow(i, this.GetRow(i) - this.GetRow(row) * (Double)this.table.Rows[i][col]);
        }
        /// <summary>Gets the reduced row echelon form of the matrix.</summary>
        /// <returns>Returns the matrix in its reduced row echelon form.</returns>
        public void RREF()
        {
            Int32 ncols = Math.Min(this.NumOfRows, this.NumOfCols);
            for (Int32 col = 0; col < ncols; col++)
            {
                this.SetNonZeroRowOrdinate(col, col);
                this.Pivot(col, col); 
            }
        }
        /// <summary>Gets the inverse of this instance.</summary>
        /// <returns>Returns the inverse of this instance.</returns>
        public Matrix Inverse()
        {
            if (this.NumOfRows != this.NumOfCols) throw new Exception("Must be a square matrix to invert.");
            Matrix newmat = this.Copy();
            newmat.AppendCols(Matrix.Identity(this.NumOfRows)); 
            newmat.RREF();
            return newmat.GetCols(this.NumOfRows, newmat.NumOfCols - 1);
        }
        /// <summary>Solves for 'x' in Ax = b.</summary>
        /// <param name="b">The righthand side of the Ax = b equation.</param>
        /// <returns>Returns the Matrix for x.</returns>
        public static Matrix SolveLinearSys(Matrix A, Matrix b)
        {
            if (A.NumOfRows != A.NumOfCols)
                throw new Exception("A needs to be a square matrix to have a unique solution.");
            if (b.NumOfRows != A.NumOfRows)
                throw new ArgumentException("b needs to have the same number of rows as A.");
            Matrix newmat = A.Copy();
            newmat.AppendCols(b);
            newmat.RREF();
            return newmat.GetCols(A.NumOfRows, newmat.NumOfCols - 1);
        }

        #endregion
        
		#region Overrides

        public override Int32 GetHashCode()
        {
            return base.GetHashCode();
        }
        /// <summary>Gets whether all elements in this matrix equal another.</summary>
        /// <param name="obj">The matrix to which to compare this matrix.</param>
        public override bool Equals(object obj)
        {
            return obj.GetType().Equals(typeof(Matrix)) && (Matrix)obj == this;
        }
        /// <summary>Writes the string of the matrix.</summary>
        /// <returns>Returns the string of the matrix.</returns>
        public override string ToString()
        {
            if (this.NumOfRows == 0 || this.NumOfCols == 0) return "[]";
            StringBuilder s = new StringBuilder();
            for (Int32 i = 0; i < this.NumOfRows; i++)
            {
                s.Append(this.table.Rows[i][0].ToString());
                for (Int32 j = 1; j < this.NumOfCols; j++)
                    s.Append(" " + ((Double)this.table.Rows[i][j]).ToString());
                s.Append("\n");
            }
            return s.ToString(0, s.Length - 1);
        }
        /// <summary>Gets a matrix from a string (similar to the way MATLAB does it).</summary>
        /// <param name="matrix">The string representing a matrix.</param>
        /// <returns>Returns the Matrix represented in the string.</returns>
        public static Matrix FromString(string matrix)
        {
            char[] brackets = new char[] {'[', ']'}; 
            char[] rowDividers = new char[] {';', '\n'}; 
            char[] colDividers = new char[] {' ', ',' };
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
            Double[,] vals = new Double[nrows, ncols]; 
            for (Int32 i = 0; i < nrows; i++)
            {
                cols = rows[i].Split(colDividers, StringSplitOptions.RemoveEmptyEntries);
                for (Int32 j = 0; j < ncols; j++)
                    vals[i, j] = Convert.ToDouble(cols[j]);
            }
            return new Matrix(vals); 
        }

        #endregion
        
		#region Operators

        // arrays
        [CLSCompliant(false)]
        public static implicit operator Matrix(Double[] v)
        {
            return new Matrix(v);
        }
        [CLSCompliant(false)]
        public static implicit operator Matrix(Double[,] a)
        {
            return new Matrix(a);
        }
        [CLSCompliant(false)]
        public static implicit operator Double[](Matrix m)
        {
            if (m == null || m.NumOfCols == 0 || m.NumOfRows == 0) return null;
            if (m.NumOfRows == 1)
                return m.GetRowArray(0);
            else
                return m.GetColArray(0);
        }
        [CLSCompliant(false)]
        public static implicit operator Double[,](Matrix m)
        {
            if (m == null) return null;
            return m.ToArray();
        }

        // strings 
        public static implicit operator string(Matrix m)
        {
            return m.ToString();
        }
        public static implicit operator Matrix(string s)
        {
            return Matrix.FromString(s);
        }
        public static string operator +(Matrix m, string s)
        {
            if (m == null) return s;
            return m.ToString() + s; 
        }
        public static string operator +(string s, Matrix m)
        {
            if (m == null) return s; 
            return s + m.ToString();
        }

        // math
        public static Matrix operator *(Matrix a, Matrix b)
        {
            return a.Mult(b, TransposeOption.None);
        }
        public static Matrix operator *(Matrix a, Double b)
        {
            return a.Mult(b);
        }
        public static Matrix operator *(Double b, Matrix a)
        {
            return a.Mult(b);
        }
        public static Matrix operator +(Matrix a, Matrix b)
        {
            return a.Add(b);
        }
        public static Matrix operator +(Matrix a, Double b)
        {
            return a.Add(b);
        }
        public static Matrix operator +(Double b, Matrix a)
        {
            return a.Add(b);
        }
        public static Matrix operator -(Matrix a, Matrix b)
        {
            return a.Add(b.Mult(-1));
        }
        public static Matrix operator -(Matrix a, Double b)
        {
            return a.Add(-b);
        }
        public static Matrix operator -(Double b, Matrix a)
        {
            return a.Mult(-1).Add(b);
        }
        public static Matrix operator /(Matrix a, Double b)
        {
            return a.Mult(1 / b);
        }
	
        // equality
        public static bool operator <=(Matrix a, Double b)
        {
            if (a == null) return false; 
            for (Int32 i = 0; i < a.NumOfRows; i++)
                for (Int32 j = 0; j < a.NumOfCols; j++)
                    if ((Double)a.table.Rows[i][j] > b)
                        return false;
            return true; 
        }
        public static bool operator >=(Matrix a, Double b)
        {
            if (a == null) return false;
            for (Int32 i = 0; i < a.NumOfRows; i++)
                for (Int32 j = 0; j < a.NumOfCols; j++)
                    if ((Double)a.table.Rows[i][j] < b)
                        return false;
            return true;
        }
        public static bool operator <(Matrix a, Double b)
        {
            if (a == null) return false;
            for (Int32 i = 0; i < a.NumOfRows; i++)
                for (Int32 j = 0; j < a.NumOfCols; j++)
                    if ((Double)a.table.Rows[i][j] >= b)
                        return false;
            return true;
        }
        public static bool operator >(Matrix a, Double b)
        {
            if (a == null) return false;
            for (Int32 i = 0; i < a.NumOfRows; i++)
                for (Int32 j = 0; j < a.NumOfCols; j++)
                    if ((Double)a.table.Rows[i][j] <= b)
                        return false;
            return true;
        }
        public static bool operator ==(Matrix a, Double b)
        {
            if (a == null) return false;
            for (Int32 i = 0; i < a.NumOfRows; i++)
                for (Int32 j = 0; j < a.NumOfCols; j++)
                    if ((Double)a.table.Rows[i][j] == b)
                        return false;
            return true;
        }
        public static bool operator !=(Matrix a, Double b)
        {
            return !(a == b);
        }
        public static bool operator <=(Matrix a, Matrix b)
        {
            if ((object)a == null && (object)b == null) return true;
            if ((object)a == null || (object)b == null) return false;
            if (a.NumOfCols != b.NumOfCols || a.NumOfRows != b.NumOfRows) 
                throw new Exception("Matrices need to have the same dimensions.");
            for (Int32 i = 0; i < a.NumOfRows; i++)
                for (Int32 j = 0; j < a.NumOfCols; j++)
                    if ((Double)a.table.Rows[i][j] > (Double)b.table.Rows[i][j])
                        return false;
            return true;
        }
        public static bool operator >=(Matrix a, Matrix b)
        {
            if ((object)a == null && (object)b == null) return true;
            if ((object)a == null || (object)b == null) return false;
            if (a.NumOfCols != b.NumOfCols || a.NumOfRows != b.NumOfRows)
                throw new Exception("Matrices need to have the same dimensions.");
            for (Int32 i = 0; i < a.NumOfRows; i++)
                for (Int32 j = 0; j < a.NumOfCols; j++)
                    if ((Double)a.table.Rows[i][j] < (Double)b.table.Rows[i][j])
                        return false;
            return true;
        }
        public static bool operator <(Matrix a, Matrix b)
        {
            if ((object)a == null || (object)b == null) return false;
            if (a.NumOfCols != b.NumOfCols || a.NumOfRows != b.NumOfRows)
                throw new Exception("Matrices need to have the same dimensions.");
            for (Int32 i = 0; i < a.NumOfRows; i++)
                for (Int32 j = 0; j < a.NumOfCols; j++)
                    if ((Double)a.table.Rows[i][j] >= (Double)b.table.Rows[i][j])
                        return false;
            return true;
        }
        public static bool operator >(Matrix a, Matrix b)
        {
            if ((object)a == null || (object)b == null) return false;
            if (a.NumOfCols != b.NumOfCols || a.NumOfRows != b.NumOfRows)
                throw new Exception("Matrices need to have the same dimensions.");
            for (Int32 i = 0; i < a.NumOfRows; i++)
                for (Int32 j = 0; j < a.NumOfCols; j++)
                    if ((Double)a.table.Rows[i][j] <= (Double)b.table.Rows[i][j])
                        return false;
            return true;
        }
        public static bool operator ==(Matrix a, Matrix b)
        {
            if ((object)a == null && (object)b == null) return true;
            if ((object)a == null || (object)b == null) return false;
            if (a.NumOfCols != b.NumOfCols || a.NumOfRows != b.NumOfRows)
                throw new Exception("Matrices need to have the same dimensions.");
            for (Int32 i = 0; i < a.NumOfRows; i++)
                for (Int32 j = 0; j < a.NumOfCols; j++)
                    if ((Double)a.table.Rows[i][j] != (Double)b.table.Rows[i][j])
                        return false;
            return true;
        }
        public static bool operator !=(Matrix a, Matrix b)
        {
            return !(a == b);
        }

        #endregion
        
		#region Built-in matrices

        /// <summary>Builds the identity matrix.</summary>
        /// <param name="n">The number of rows and columns in the identity matrix.</param>
        /// <returns>Returns the identity matrix.</returns>
        public static Matrix Identity(Int32 n)
        {
            Matrix I = new Matrix(n, n, 0);
            for (Int32 i = 0; i < I.NumOfRows; i++)
                I.table.Rows[i][i] = 1;
            return I;
        }
        /// <summary>Generates a matrix with random numbers between 0.0 and 1.0</summary>
        /// <param name="nrows">Number of rows in the matrix</param>
        /// <param name="ncols">Number of columns in the matrix</param>
        public static Matrix Random(Int32 nrows, Int32 ncols)
        {
            Random rng = new Random(); 
            Matrix m = new Matrix(nrows, ncols, 0.0);
            for (int i = 0; i < m.NumOfRows; i++)
                for (int j = 0; j < m.NumOfCols; j++)
                    m[i, j] = rng.NextDouble();
            return m; 
        }
        /// <summary>Generates a matrix with random, nonnegative integers</summary>
        /// <param name="nrows">Number of rows in the matrix</param>
        /// <param name="ncols">Number of columns in the matrix</param>
        public static Matrix RandomInt(Int32 nrows, Int32 ncols)
        {
            Random rng = new Random();
            Matrix m = new Matrix(nrows, ncols, 0.0);
            for (int i = 0; i < m.NumOfRows; i++)
                for (int j = 0; j < m.NumOfCols; j++)
                    m[i, j] = rng.Next();
            return m;
        }
        /// <summary>Generates a matrix with random integers between 0 (inclusive) and maxValue (exclusive)</summary>
        /// <param name="nrows">Number of rows in the matrix</param>
        /// <param name="ncols">Number of columns in the matrix</param>
        public static Matrix RandomInt(Int32 nrows, Int32 ncols, int maxValue)
        {
            Random rng = new Random();
            Matrix m = new Matrix(nrows, ncols, 0.0);
            for (int i = 0; i < m.NumOfRows; i++)
                for (int j = 0; j < m.NumOfCols; j++)
                    m[i, j] = rng.Next(maxValue);
            return m;
        }
        /// <summary>Generates a matrix with random integers between minValue (inclusive) and maxValue (exclusive)</summary>
        /// <param name="nrows">Number of rows in the matrix</param>
        /// <param name="ncols">Number of columns in the matrix</param>
        public static Matrix RandomInt(Int32 nrows, Int32 ncols, int minValue, int maxValue)
        {
            Random rng = new Random();
            Matrix m = new Matrix(nrows, ncols, 0.0);
            for (int i = 0; i < m.NumOfRows; i++)
                for (int j = 0; j < m.NumOfCols; j++)
                    m[i, j] = rng.Next(minValue, maxValue);
            return m;
        }

        #endregion
    }

}
