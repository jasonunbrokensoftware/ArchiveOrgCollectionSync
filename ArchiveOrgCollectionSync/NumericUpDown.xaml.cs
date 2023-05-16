namespace ArchiveOrgCollectionSync
{
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;

    public partial class NumericUpDown
    {
        public NumericUpDown()
        {
            this.InitializeComponent();
            this.MaxValue = 100;
            this.TextBox.Text = this.Value.ToString();
        }

        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
            nameof(NumericUpDown.MinValue),
            typeof(int),
            typeof(NumericUpDown));

        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
            nameof(NumericUpDown.MaxValue),
            typeof(int),
            typeof(NumericUpDown));

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(NumericUpDown.Value),
            typeof(int),
            typeof(NumericUpDown));

        public int MinValue
        {
            get
            {
                return (int)this.GetValue(NumericUpDown.MinValueProperty);
            }

            set
            {
                this.SetValue(NumericUpDown.MinValueProperty, value);
            }
        }

        public int MaxValue
        {
            get
            {
                return (int)this.GetValue(NumericUpDown.MaxValueProperty);
            }

            set
            {
                this.SetValue(NumericUpDown.MaxValueProperty, value);
            }
        }

        public int Value
        {
            get
            {
                return (int)this.GetValue(NumericUpDown.ValueProperty);
            }

            set
            {
                this.SetValue(NumericUpDown.ValueProperty, value);
            }
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            int value = this.Value + 1;

            if (value > this.MaxValue)
            {
                return;
            }

            this.TextBox.Text = value.ToString();
            this.Value = value;
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            int value = this.Value - 1;

            if (value < this.MinValue)
            {
                return;
            }

            this.TextBox.Text = value.ToString();
            this.Value = value;
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                this.UpButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(this.UpButton, new object[] { true });
            }
            
            if (e.Key == Key.Down)
            {
                this.DownButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(this.DownButton, new object[] { true });
            }
        }

        private void TextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(this.UpButton, new object[] { false });
            }

            if (e.Key == Key.Down)
            {
                typeof(Button).GetMethod("set_IsPressed", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(this.DownButton, new object[] { false });
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int value = 0;

            if (!string.IsNullOrWhiteSpace(this.TextBox.Text))
            {
                if (!int.TryParse(this.TextBox.Text, out value))
                {
                    this.TextBox.Text = this.Value.ToString();
                }
            }

            if (value > this.MaxValue)
            {
                this.TextBox.Text = this.MaxValue.ToString();
            }

            if (value < this.MinValue)
            {
                this.TextBox.Text = this.MinValue.ToString();
            }

            this.TextBox.SelectionStart = this.TextBox.Text.Length;

            if (!string.IsNullOrWhiteSpace(this.TextBox.Text))
            {
                this.Value = int.Parse(this.TextBox.Text);
            }
        }

        private void NumericUpDown_OnLoaded(object sender, RoutedEventArgs e)
        {
            this.TextBox.Text = this.Value.ToString();
        }
    }
}