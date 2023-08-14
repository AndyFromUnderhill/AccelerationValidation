using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace AccelerationValidation
{
  public delegate void InvalidAccelEventHandler(object source, InvalidAccelEventArgs e);
  public class InvalidAccelEventArgs : EventArgs
  {
    public int RPMTried { get; set; }
    public int TimeTried { get; set; }
  }

  /// <summary>
  /// Interaction logic for DefineProtocol.xaml
  /// </summary>
  public partial class DefineProtocol : Window
  {
    public CProtocol theProt
    { get; set; }
    const int DEF_TIME_JUMP = 2; // default seconds between transitions
    const int DEF_LOAD_SPEED = 4; // default load speed in RPM

    public DefineProtocol()
    {
      InitializeComponent();

      theProt = new CProtocol("Untitled Protocol");

      MakeBindings();

      //CProtocol.InvalidAccel += c_InvalidAccel;

      CommonClass.tmHub.Subscribe<MyMessage1>((m) => { InvalidAccelHandler1(m.Sender, m.Content); });
      CommonClass.tmHub.Subscribe<MyMessage2>((m) => { InvalidAccelHandler2(m.Sender, m.Content); });
    }
    private void MakeBindings()
    {
      this.DataContext = theProt;
      theProt.Transitions.CollectionChanged += Transitions_CollectionChanged;

      this.AddHandler(Validation.ErrorEvent, new RoutedEventHandler(OnErrorEvent));
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      // set the load rpm to default 4 RPM
      theProt.LoadSpeed = DEF_LOAD_SPEED;

      // add the first transition - time = 0, speed = load speed
      theProt.AddTransition(new CTransition(0, theProt.LoadSpeed));
    }

    private void gridTransitions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (gridTransitions.SelectedIndex != 0)  // don't allow to delete the first transition at time 0
      {
        btnDelTrans.IsEnabled = gridTransitions.SelectedItems.Count > 0 &&
                                ((CTransition)gridTransitions.SelectedItem).Time != 0;
      }
    }
    private void gridTransitions_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
      // cancel or commit?
      if (e.EditAction != System.Windows.Controls.DataGridEditAction.Commit)
        return;

      //resort the list
      theProt.Transitions.Sort();

      // update the protocol duration
      UpdateDuration();
    }
    private void Transitions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      // might have deleted a transition, fix the duration
      UpdateDuration();
    }
    private void UpdateDuration()
    {
      // set the duration to the last time
      theProt.Duration = new TimeSpan(0, 0, Convert.ToInt32(theProt.Transitions[theProt.Transitions.Count - 1].Time));
    }
    private void gridTransitions_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
      if (((CTransition)e.Row.Item).Time == 0 && e.Column.Header.ToString() == "Time (s)")
      {
        // don't allow user to edit the time "0" transition
        MessageBox.Show("This transition's time may not be changed.  There must be one transition with time 0 (zero).",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Cancel = true;
      }
    }
    void btnAddTrans_Click(object sender, RoutedEventArgs e)
    {
      int last = -DEF_TIME_JUMP;
      try
      {
        last = theProt.Transitions.Last().Time;
      }
      catch { };
      CTransition trans = new CTransition(last + DEF_TIME_JUMP, 0);
      theProt.AddTransition(trans);
      gridTransitions.UpdateLayout(); // refresh the grid
      gridTransitions.ScrollIntoView(trans);  // scroll to the new row

      // select the time cell for editing
      DataGridCellInfo cellinfo = new DataGridCellInfo(trans, gridTransitions.Columns[0]);
      gridTransitions.CurrentCell = cellinfo;
      gridTransitions.BeginEdit();
    }

    private void btnDelTrans_Click(object sender, RoutedEventArgs e)
    {
      // don't allow to delete the transition with time 0
      if (0 == ((CTransition)gridTransitions.SelectedItem).Time)
      {
        MessageBox.Show("Protocols must have one transition with time 0 (zero).",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
        return;
      }

      int SelIndex = gridTransitions.SelectedIndex;
      try
      {
        // remove the transition from the transition collection
        theProt.DeleteTransition((CTransition)gridTransitions.SelectedItem);

        //refresh the contents to remove any errors on the deleted row
        gridTransitions.ItemsSource = null;
        gridTransitions.ItemsSource = theProt.Transitions;

        // Enable OK button if errorCount == 0
        btnOk.IsEnabled = errorCount == 0;

        // select the row below the deleted guy, unless it was the last one
        gridTransitions.SelectedIndex = SelIndex > gridTransitions.Items.Count - 1 ?
                                        gridTransitions.Items.Count - 1 :
                                        SelIndex;
        gridTransitions.ScrollIntoView(gridTransitions.SelectedItem, gridTransitions.Columns[0]);
      }
      catch (Exception x)
      {
        System.Diagnostics.Debug.WriteLine(x.ToString());
      }

      // set the duration to the last time (last time may have changed if deleting the last trans)
      UpdateDuration();
    }
    public string TheError
    {
      private get { return (string)GetValue(TheErrorProperty); }
      set { SetValue(TheErrorProperty, value); }
    }

    public static readonly DependencyProperty TheErrorProperty =
        DependencyProperty.Register("TheError", typeof(string), typeof(DefineProtocol));

    private int errorCount;
    private void OnErrorEvent(object sender, RoutedEventArgs e)
    {
      var validationEventArgs = e as ValidationErrorEventArgs;
      if (validationEventArgs == null)
        throw new Exception("Unexpected event args");

      switch (validationEventArgs.Action)
      {
        case ValidationErrorEventAction.Added:
          {
            // set the Error message for the tooltip bindings
            TheError = validationEventArgs.Error.ErrorContent.ToString();
            errorCount++;
            break;
          }
        case ValidationErrorEventAction.Removed:
          {
            errorCount--;
            break;
          }
        default:
          {
            throw new Exception("Unknown action");
          }
      }

      // set the OK button IsEnabled status based on the error count
      btnOk.IsEnabled = errorCount == 0;
    }
    private void InvalidAccelHandler1(object sender, InvalidAccelEventArgs e)
    {
      System.Diagnostics.Debug.WriteLine("InvalidAccelHandler1 sender: " + sender.ToString());
      System.Diagnostics.Debug.WriteLine("                    content: " + e.ToString());
      System.Diagnostics.Debug.WriteLine("                  RPM Tried: " + e.RPMTried.ToString());
      System.Diagnostics.Debug.WriteLine("                 Time Tried: " + e.TimeTried.ToString());

      RoutedEventArgs rea = new RoutedEventArgs();
      rea.Source = sender;
      this.OnErrorEvent(sender, rea );
    }
    private void InvalidAccelHandler2(object sender, string content)
    {
      System.Diagnostics.Debug.WriteLine("InvalidAccelHandler2 sender: " + sender.ToString());
      System.Diagnostics.Debug.WriteLine("                    content: " + content);

      //ValidationErrorEventArgs veea = new ValidationErrorEventArgs();
      //ValidationError verr = new ValidationError(AccelerationValidationRule, gridTransitions.GetBindingExpression(DataGrid.BindingGroupProperty));
      //verr.ErrorContent = "Content string added in DefineProtocol::InvalidAccelHandler2";
      ////verr.Action = ValidationErrorEventAction.Added;

      //this.OnErrorEvent(sender, verr);
      //this.OnErrorEvent(sender, new RoutedEventArgs(new ValidationError(AccelerationValidation, bindingInError), sender));
    }

    private void btnOk_Click(object sender, RoutedEventArgs e)
    {
      MessageBox.Show("OK button clicked.");
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
      MessageBox.Show("Cancel button clicked.");
    }
  }

  [ValueConversion(typeof(Boolean), typeof(Boolean))]
  public class BoolToOppositeBoolConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (targetType != typeof(bool) && targetType != typeof(System.Nullable<bool>))
      {
        throw new InvalidOperationException("The target must be a boolean");
      }
      if (null == value)
      {
        return null;
      }
      return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (targetType != typeof(bool) && targetType != typeof(System.Nullable<bool>))
      {
        throw new InvalidOperationException("The target must be a boolean");
      }
      if (null == value)
      {
        return null;
      }
      return !(bool)value;
    }
  }
}
