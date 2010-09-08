﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ClosedXML.Excel
{
    public interface IXLRange: IXLStylized
    {
        IXLRange Row(Int32 row);
        IXLRange Column(Int32 column);
        IXLRange Column(String column);
        Int32 RowNumber { get; }
        Int32 ColumnNumber { get; }
        String ColumnLetter { get; }
        IXLRangeInternals Internals { get; }
    }

    public static class IXLRangeMethods
    {
        public static IXLCell FirstCell(this IXLRange range)
        {
            return range.Cell(1, 1);
        }
        public static IXLCell LastCell(this IXLRange range)
        {
            return range.Cell(range.RowCount(), range.ColumnCount());
        }

        public static IXLCell Cell(this IXLRange range, IXLAddress cellAddressInRange)
        {
            IXLAddress absoluteAddress = (XLAddress)cellAddressInRange + (XLAddress)range.Internals.FirstCellAddress - 1;
            if (range.Internals.Worksheet.Internals.CellsCollection.ContainsKey(absoluteAddress))
            {
                return range.Internals.Worksheet.Internals.CellsCollection[absoluteAddress];
            }
            else
            {
                var newCell = new XLCell(absoluteAddress, range.Style);
                range.Internals.Worksheet.Internals.CellsCollection.Add(absoluteAddress, newCell);
                return newCell;
            }
        }
        public static IXLCell Cell(this IXLRange range, Int32 row, Int32 column)
        {
            return range.Cell(new XLAddress(row, column));
        }
        public static IXLCell Cell(this IXLRange range, Int32 row, String column)
        {
            return range.Cell(new XLAddress(row, column));
        }
        public static IXLCell Cell(this IXLRange range, String cellAddressInRange)
        {
            return range.Cell(new XLAddress(cellAddressInRange));
        }

        public static Int32 RowCount(this IXLRange range)
        {
            return range.Internals.LastCellAddress.Row - range.Internals.FirstCellAddress.Row + 1;
        }
        public static Int32 ColumnCount(this IXLRange range)
        {
            return range.Internals.LastCellAddress.Column - range.Internals.FirstCellAddress.Column + 1;
        }

        public static IXLRange Range(this IXLRange range, Int32 firstCellRow, Int32 firstCellColumn, Int32 lastCellRow, Int32 lastCellColumn)
        {
            return range.Range(new XLAddress(firstCellRow, firstCellColumn), new XLAddress(lastCellRow, lastCellColumn));
        }
        public static IXLRange Range(this IXLRange range, String rangeAddress)
        {
            String[] arrRange = rangeAddress.Split(':');
            return range.Range(arrRange[0], arrRange[1]);
        }
        public static IXLRange Range(this IXLRange range, String firstCellAddress, String lastCellAddress)
        {
            return range.Range(new XLAddress(firstCellAddress), new XLAddress(lastCellAddress));
        }
        public static IXLRange Range(this IXLRange range, IXLAddress firstCellAddress, IXLAddress lastCellAddress)
        {
            var newFirstCellAddress = (XLAddress)firstCellAddress + (XLAddress)range.Internals.FirstCellAddress - 1;
            var newLastCellAddress = (XLAddress)lastCellAddress + (XLAddress)range.Internals.FirstCellAddress - 1;
            var xlRangeParameters = new XLRangeParameters(newFirstCellAddress, newLastCellAddress, range.Internals.Worksheet, range.Style);
            if (
                   newFirstCellAddress.Row < range.Internals.FirstCellAddress.Row
                || newFirstCellAddress.Row > range.Internals.LastCellAddress.Row
                || newLastCellAddress.Row > range.Internals.LastCellAddress.Row
                || newFirstCellAddress.Column < range.Internals.FirstCellAddress.Column
                || newFirstCellAddress.Column > range.Internals.LastCellAddress.Column
                || newLastCellAddress.Column > range.Internals.LastCellAddress.Column
                )
                throw new ArgumentOutOfRangeException();

            return new XLRange(xlRangeParameters);
        }
        public static IXLRange Range(this IXLRange range, IXLCell firstCell, IXLCell lastCell)
        {
            return range.Range(firstCell.Address, lastCell.Address);
        }

        public static IEnumerable<IXLCell> Cells(this IXLRange range)
        {
            foreach(var row in Enumerable.Range(1, range.RowCount())) 
            {
                foreach(var column in Enumerable.Range(1, range.ColumnCount()))
                {
                    yield return range.Cell(row, column);
                }
            }
        }

        public static void Merge(this IXLRange range)
        {
            var mergeRange = range.Internals.FirstCellAddress.ToString() + ":" + range.Internals.LastCellAddress.ToString();
            if (!range.Internals.Worksheet.Internals.MergedCells.Contains(mergeRange))
                range.Internals.Worksheet.Internals.MergedCells.Add(mergeRange);
        }
        public static void Unmerge(this IXLRange range)
        {
            range.Internals.Worksheet.Internals.MergedCells.Remove(range.Internals.FirstCellAddress.ToString() + ":" + range.Internals.LastCellAddress.ToString());
        }

        public static IXLRange FirstColumn(this IXLRange range)
        {
            return range.Column(1);
        }
        public static IXLRange LastColumn(this IXLRange range)
        {
            return range.Column(range.ColumnCount());
        }
        public static IXLRange FirstRow(this IXLRange range)
        {
            return range.Row(1);
        }
        public static IXLRange LastRow(this IXLRange range)
        {
            return range.Row(range.RowCount());
        }

        public static void InsertRowsBelow(this IXLRange range, Int32 numberOfRows)
        {
            var cellsToInsert = new Dictionary<IXLAddress, IXLCell>();
            var cellsToDelete = new List<IXLAddress>();
            var lastRow = range.LastRow().RowNumber;
            var firstColumn = range.FirstColumn().ColumnNumber;
            var lastColumn = range.LastColumn().ColumnNumber;
            foreach (var c in range.Internals.Worksheet.Internals.CellsCollection
                .Where(c =>
                c.Key.Row > lastRow
                && c.Key.Column >= firstColumn
                && c.Key.Column <= lastColumn
                ))
            {
                var newRow = c.Key.Row + numberOfRows;
                var newKey = new XLAddress(newRow, c.Key.Column);
                var newCell = new XLCell(newKey, c.Value.Style);
                newCell.Value = c.Value.Value;
                cellsToInsert.Add(newKey, newCell);
                cellsToDelete.Add(c.Key);
            }
            cellsToDelete.ForEach(c => range.Internals.Worksheet.Internals.CellsCollection.Remove(c));
            cellsToInsert.ForEach(c => range.Internals.Worksheet.Internals.CellsCollection.Add(c.Key, c.Value));
        }
        public static void InsertRowsAbove(this IXLRange range, Int32 numberOfRows)
        {
            var cellsToInsert = new Dictionary<IXLAddress, IXLCell>();
            var cellsToDelete = new List<IXLAddress>();
            var firstRow = range.FirstRow().RowNumber;
            var firstColumn = range.FirstColumn().ColumnNumber;
            var lastColumn = range.LastColumn().ColumnNumber;
            foreach (var c in range.Internals.Worksheet.Internals.CellsCollection
                .Where(c =>
                c.Key.Row >= firstRow
                && c.Key.Column >= firstColumn
                && c.Key.Column <= lastColumn
                ))
            {
                var newRow = c.Key.Row + numberOfRows;
                var newKey = new XLAddress(newRow, c.Key.Column);
                var newCell = new XLCell(newKey, c.Value.Style);
                newCell.Value = c.Value.Value;
                cellsToInsert.Add(newKey, newCell);
                cellsToDelete.Add(c.Key);
            }
            cellsToDelete.ForEach(c => range.Internals.Worksheet.Internals.CellsCollection.Remove(c));
            cellsToInsert.ForEach(c => range.Internals.Worksheet.Internals.CellsCollection.Add(c.Key, c.Value));
        }

        public static void InsertColumnsAfter(this IXLRange range, Int32 numberOfColumns)
        {
            var cellsToInsert = new Dictionary<IXLAddress, IXLCell>();
            var cellsToDelete = new List<IXLAddress>();
            var firstRow = range.FirstRow().RowNumber;
            var lastRow = range.LastRow().RowNumber;
            var lastColumn = range.LastColumn().ColumnNumber;
            foreach (var c in range.Internals.Worksheet.Internals.CellsCollection
                .Where(c =>
                c.Key.Column > lastColumn
                && c.Key.Row >= firstRow
                && c.Key.Row <= lastRow
                ))
            {
                var newColumn = c.Key.Column + numberOfColumns;
                var newKey = new XLAddress(c.Key.Row, newColumn);
                var newCell = new XLCell(newKey, c.Value.Style);
                newCell.Value = c.Value.Value;
                cellsToInsert.Add(newKey, newCell);
                cellsToDelete.Add(c.Key);
            }
            cellsToDelete.ForEach(c => range.Internals.Worksheet.Internals.CellsCollection.Remove(c));
            cellsToInsert.ForEach(c => range.Internals.Worksheet.Internals.CellsCollection.Add(c.Key, c.Value));
        }
        public static void InsertColumnsBefore(this IXLRange range, Int32 numberOfColumns)
        {
            var cellsToInsert = new Dictionary<IXLAddress, IXLCell>();
            var cellsToDelete = new List<IXLAddress>();
            var firstRow = range.FirstRow().RowNumber;
            var lastRow = range.LastRow().RowNumber;
            var firstColumn = range.FirstColumn().ColumnNumber;
            foreach (var c in range.Internals.Worksheet.Internals.CellsCollection
                .Where(c =>
                c.Key.Column >= firstColumn
                && c.Key.Row >= firstRow
                && c.Key.Row <= lastRow
                ))
            {
                var newColumn = c.Key.Column + numberOfColumns;
                var newKey = new XLAddress(c.Key.Row, newColumn);
                var newCell = new XLCell(newKey, c.Value.Style);
                newCell.Value = c.Value.Value;
                cellsToInsert.Add(newKey, newCell);
                cellsToDelete.Add(c.Key);
            }
            cellsToDelete.ForEach(c => range.Internals.Worksheet.Internals.CellsCollection.Remove(c));
            cellsToInsert.ForEach(c => range.Internals.Worksheet.Internals.CellsCollection.Add(c.Key, c.Value));
        }

        public static List<IXLRange> Columns(this IXLRange range)
        {
            var retVal = new List<IXLRange>();
            foreach (var c in Enumerable.Range(1, range.ColumnCount()))
            {
                retVal.Add(range.Column(c));
            }
            return retVal;
        }

        public static void SetAsPrintArea(this IXLRange range)
        {
            range.Internals.Worksheet.PrintOptions.PrintArea = range;
        }
    }
}

