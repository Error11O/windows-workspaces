namespace workspaces;

internal sealed class SettingsForm : Form
{
    internal SettingsForm(AppSettings settings, Icon icon, Action<AppSettings> save)
    {
        Text = "workspaces Settings";
        Icon = icon;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(460, 200);

        var modifier = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        modifier.Items.AddRange(["Windows key", "Left Alt"]);
        modifier.SelectedIndex = (int)settings.Modifier;
        var count = new NumericUpDown { Minimum = 1, Maximum = 10, Value = settings.DesktopCount, Width = 80 };
        var saveButton = new Button { Text = "Save", AutoSize = true };
        saveButton.Click += (_, _) =>
        {
            try
            {
                save(new AppSettings { Modifier = (ShortcutModifier)modifier.SelectedIndex,
                    DesktopCount = (int)count.Value });
                Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, $"Could not save settings: {exception.Message}", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        cancel.Click += (_, _) => Close();
        AcceptButton = saveButton;
        CancelButton = cancel;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 4 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.Controls.Add(new Label { Text = "Shortcut key:", AutoSize = true }, 0, 0);
        layout.Controls.Add(modifier, 1, 0);
        layout.Controls.Add(new Label { Text = "Number of desktops:", AutoSize = true }, 0, 1);
        layout.Controls.Add(count, 1, 1);
        var explanation = new Label { Text = "New application windows open maximized when the app supports it.",
            Dock = DockStyle.Fill, AutoSize = true };
        layout.Controls.Add(explanation, 0, 2);
        layout.SetColumnSpan(explanation, 2);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        actions.Controls.AddRange([cancel, saveButton]);
        layout.Controls.Add(actions, 0, 3);
        layout.SetColumnSpan(actions, 2);
        Controls.Add(layout);
    }
}
