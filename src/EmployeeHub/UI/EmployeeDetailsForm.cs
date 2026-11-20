using System.Drawing;
using System.Windows.Forms;
using EmployeeHub.Domain;

namespace EmployeeHub.UI;

internal sealed class EmployeeDetailsForm : Form
{
    public EmployeeDetailsForm(EmployeeRecord employee)
    {
        Text = "Employee details";
        Name = "EmployeeDetailsForm";
        AccessibleName = "Employee details";
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(480, 390);
        Size = new Size(540, 430);

        var details = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(18)
        };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var fields = new (string Label, string Value)[]
        {
            ("Employee ID", employee.Id.ToString()),
            ("First name", employee.FirstName),
            ("Last name", employee.LastName),
            ("Email", employee.Email),
            ("Department", employee.Department),
            ("Position", employee.PositionTitle),
            ("Employment status", employee.IsActive ? "Active" : "Inactive"),
            ("Hire date", employee.HireDate.ToString("d"))
        };
        for (var index = 0; index < fields.Length; index++)
        {
            details.RowStyles.Add(new RowStyle(SizeType.Percent, 12.5f));
            details.Controls.Add(new Label
            {
                Text = fields[index].Label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                AccessibleName = fields[index].Label
            }, 0, index);
            details.Controls.Add(new Label
            {
                Text = fields[index].Value,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                AccessibleName = fields[index].Label + " value"
            }, 1, index);
        }
        var close = FormUtilities.CreateButton("CloseDetailsButton", "Close", "Close employee details");
        close.DialogResult = DialogResult.OK;
        close.Anchor = AnchorStyles.Right;
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        footer.Controls.Add(close);
        Controls.Add(details);
        Controls.Add(footer);
        AcceptButton = close;
    }
}
