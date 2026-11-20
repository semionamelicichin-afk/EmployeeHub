using System.Drawing;
using System.Windows.Forms;

namespace EmployeeHub.UI;

internal static class FormUtilities
{
    public static DataGridView CreateGrid(string accessibleName)
    {
        var grid = new DataGridView
        {
            Name = accessibleName,
            AccessibleName = accessibleName,
            AccessibleDescription = $"Sortable {accessibleName.ToLowerInvariant()} list",
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            EnableHeadersVisualStyles = true
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Id",
            HeaderText = "ID",
            DataPropertyName = "Id",
            FillWeight = 8,
            SortMode = DataGridViewColumnSortMode.Automatic
        });
        return grid;
    }

    public static DataGridViewTextBoxColumn AddTextColumn(DataGridView grid, string name, string header, int fillWeight = 15)
    {
        var column = new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            DataPropertyName = name,
            FillWeight = fillWeight,
            SortMode = DataGridViewColumnSortMode.Automatic
        };
        grid.Columns.Add(column);
        return column;
    }

    public static Button CreateButton(string name, string text, string description)
    {
        return new Button
        {
            Name = name,
            Text = text,
            AccessibleName = text,
            AccessibleDescription = description,
            AutoSize = true,
            MinimumSize = new Size(92, 32),
            Margin = new Padding(4),
            UseVisualStyleBackColor = true
        };
    }

    public static ComboBox CreateDropDown(string name, string accessibleName, IEnumerable<string> values)
    {
        var combo = new ComboBox
        {
            Name = name,
            AccessibleName = accessibleName,
            AccessibleDescription = accessibleName,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 170,
            Margin = new Padding(4),
            IntegralHeight = false
        };
        combo.Items.AddRange(values.Cast<object>().ToArray());
        if (combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }
        return combo;
    }

    public static TableLayoutPanel CreateEditorLayout(int rowCount)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = rowCount,
            Padding = new Padding(16),
            AutoSize = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var index = 0; index < rowCount; index++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        }
        return layout;
    }

    public static void AddEditorField(TableLayoutPanel layout, int row, string labelText, Control editor)
    {
        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            AccessibleName = labelText
        };
        editor.Dock = DockStyle.Fill;
        editor.Margin = new Padding(3, 5, 3, 5);
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(editor, 1, row);
    }

    public static void AddActionRow(TableLayoutPanel layout, int row, Control action)
    {
        action.Dock = DockStyle.Right;
        layout.Controls.Add(action, 1, row);
    }

    public static void ShowError(IWin32Window owner, string message) =>
        MessageBox.Show(owner, message, "EmployeeHub", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
