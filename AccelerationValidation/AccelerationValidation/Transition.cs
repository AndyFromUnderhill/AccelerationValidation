using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Data;
using System.ComponentModel;
using System.Windows;
using TinyMessenger;

namespace AccelerationValidation
{
  [Serializable]
  public class CTransition : DependencyObject, INotifyPropertyChanged, IComparable//, IDataErrorInfo
  {
    public const int MIN_RPM = -100;
    public const int MAX_RPM = 100;
    public const int MAX_ACCEL = 10; // rpm/sec
    public const int MAX_DECEL = -10; // rpm/sec

    private int time;
    public int Time
    {
      get { return time; }
      set
      {
        time = value;
        NotifyPropertyChanged("Time");
      }
    }

    private int rpm;
    public int RPM
    {
      get { return rpm; }
      set
      {
        rpm = value;
        NotifyPropertyChanged("RPM");
      }
    }
    public CTransition()
    {
    }

    public CTransition(int seconds, int rpm)
    {
      Time = seconds;
      RPM = rpm;
    }

    public int CompareTo(object obj)
    {
      CTransition trans = obj as CTransition;
      if (trans == null)
      {
        throw new ArgumentException("Object is not a CTransition");
      }
      return Time.CompareTo(trans.Time);
    }

    [NonSerialized]
    private static LinkedList<CTransition> llValid;
    public static void SetValidationTransitionList(ICollection<CTransition> list)
    {
      llValid = new LinkedList<CTransition>(list);
    }

    #region INotifyPropertyChanged Members
    public event PropertyChangedEventHandler PropertyChanged;
    #endregion

    #region Private Helpers
    private void NotifyPropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }
    #endregion

    public static bool TransitionTimeExists(int val)
    {
      bool bRet = false;
      if (llValid != null)
        bRet = llValid.Any(m => m.Time == val);
      return bRet;
    }

    public static double GetAccelDecel(CTransition tProp, bool bForward = true)
    {
      double dRet = 0.0;  // default to no accel/decel

      // find this proposed transition tProp in the list of transitions, and compare to the one before it, or after it
      try
      {
        LinkedListNode<CTransition> tFound = llValid.Find(tProp);
        if (tFound != null)
        {
          if (bForward && tFound.Next != null)
          {
            // divide by zero results in dRet == INFINITY  (only have to cast first value to make result a double)
            dRet = (double)(tFound.Next.Value.RPM - tFound.Value.RPM) / (tFound.Next.Value.Time - tFound.Value.Time);
          }
          if (!bForward && tFound.Previous != null)
          {
            // divide by zero results in dRet == INFINITY (only have to cast first value to make result a double)
            dRet = (double)(tFound.Value.RPM - tFound.Previous.Value.RPM) / (tFound.Value.Time - tFound.Previous.Value.Time);
          }
        }
      }
      catch (Exception x)//InvalidOperationException
      {
        string msg = x.Message;
        if (x.InnerException != null)
          msg += "\r\n" + x.InnerException.ToString();
        MessageBox.Show(msg);
        return dRet;
      }
      return dRet;
    }

  }
  internal class TimeValidationRule : ValidationRule
  {
    private int _lastProposedValue = -1;

    public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
    {
      if (null == value)
      {
        return new ValidationResult(false, "Time must be a whole number.");
      }
      else
      {
        int proposedValue;
        if (!int.TryParse(value.ToString(), out proposedValue))
        {
          return new ValidationResult(false, "Time must be a whole number, '" + value.ToString() + "' is not a whole number.");
        }

        if (proposedValue > CProtocol.MAX_PROTOCOL_SECONDS)
        {
          // Something was wrong.
          string str = string.Format("Protocols are limited to {0} seconds ({1} hours)",
            CProtocol.MAX_PROTOCOL_SECONDS, CProtocol.MAX_PROTOCOL_SECONDS / 60 / 60);
          return new ValidationResult(false, str);
        }
        if (proposedValue < 0)
        {
          return new ValidationResult(false, "Time must be a positive integer (whole number).");
        }

        if (proposedValue != _lastProposedValue && CTransition.TransitionTimeExists(proposedValue))
        {
          string str = string.Format("A transition with time {0} already exists.", proposedValue);
          return new ValidationResult(false, str);
        }
        _lastProposedValue = proposedValue;
      }
      // Everything OK.
      return new ValidationResult(true, "time is ok");
    }
  }

  internal class SpeedValidationRule : ValidationRule
  {
    public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
    {
      if (value != null)
      {
        int proposedValue;
        if (!int.TryParse(value.ToString(), out proposedValue))
        {
          return new ValidationResult(false, "'" + value.ToString() + "' is not a whole number.");
        }
        if (proposedValue > CTransition.MAX_RPM)
        {
          string str = string.Format("'{0}' is an invalid speed.  {1} is the maximum rpm.", proposedValue, CTransition.MAX_RPM);
          return new ValidationResult(false, str);
        }
        if (proposedValue < CTransition.MIN_RPM)
        {
          string str = string.Format("'{0}' is an invalid speed.  {1} is the minimum rpm.", proposedValue, CTransition.MIN_RPM);
          return new ValidationResult(false, str);
        }
      }

      // Everything OK.
      return new ValidationResult(true, null);
    }
  }

  internal class AccelerationValidationRule : ValidationRule
  {
    private T GetParent<T>(DependencyObject d) where T : class
    {
      while (d != null & !(d is T))
      {
        d = System.Windows.Media.VisualTreeHelper.GetParent(d);
      }
      return d as T;
    }

    public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
    {
      if (value is BindingGroup)
      {
        System.Windows.Data.BindingGroup bg = value as BindingGroup;
        if (bg != null)
        {
          CTransition ct = (CTransition)bg.Items[0];

          DataGridRow dgRow = bg.Owner as DataGridRow;
          if (dgRow != null)
          {
            DataGrid dg = GetParent<DataGrid>(dgRow);
            // do some stuff here to compare against other rows - get rows from DataGrid?
            foreach (CTransition ct1 in dg.Items)
            {
              System.Diagnostics.Debug.WriteLine(ct1.Time.ToString() + " " + ct1.RPM.ToString());
            }
          }

          double dAccelToNext = CTransition.GetAccelDecel(ct, true);
          double dAccelFromPrev = CTransition.GetAccelDecel(ct, false);

          string str = string.Empty;

          // warn about change from previous step
          if (dAccelFromPrev > CTransition.MAX_ACCEL)
          {
            str = string.Format("'{0}' is an invalid speed.  {1} rpm/s is the maximum acceleration.\r\n\r\nAcceleration from previous transition is {2:F2} rpm/s.",
              ct.RPM, CTransition.MAX_ACCEL, dAccelFromPrev);
          }
          if (dAccelFromPrev < CTransition.MAX_DECEL)
          {
            str = string.Format("'{0}' is an invalid speed.  {1} rpm/s is the maximum deceleration.\r\n\r\nDeceleration from previous transition is {2:F2} rpm/s.",
              ct.RPM, CTransition.MAX_DECEL, dAccelFromPrev);
          }
          // warn about change to next step
          if (dAccelToNext > CTransition.MAX_ACCEL)
          {
            str = string.Format("'{0}' is an invalid speed.  {1} rpm/s is the maximum acceleration.\r\n\r\nAcceleration to next transition is {2:F2} rpm/s.",
              ct.RPM, CTransition.MAX_ACCEL, dAccelToNext);
          }
          if (dAccelToNext < CTransition.MAX_DECEL)
          {
            str = string.Format("'{0}' is an invalid speed.  {1} rpm/s is the maximum deceleration.\r\n\r\nDeceleration to transition is {2:F2} rpm/s.",
              ct.RPM, CTransition.MAX_DECEL, dAccelToNext);
          }
          if ( !String.IsNullOrEmpty(str) )
          {
            // publish event with InvalidAccelEventArgs parameter
            InvalidAccelEventArgs iaea = new InvalidAccelEventArgs();
            iaea.RPMTried = ct.RPM;
            iaea.TimeTried = ct.Time;
            CommonClass.tmHub.Publish(new MyMessage1(this, iaea));

            // publish event because ValidationResult(false...) line doesn't publish Validation.ErrorEvent
            CommonClass.tmHub.Publish(new MyMessage2(this, str));

            // publish event with ValidationErrorEventArgs();
            //ValidationError theerr = new ValidationError(AccelerationValidationRule, bindingInError);   //this line won't compile, says param1 is a type, which is not valid in the given context
                                                                                                          // however, AccelerationValidationRule is a class?
            //ValidationErrorEventArgs veea = new ValidationErrorEventArgs(theerr, ValidationErrorEventAction.Added);
            //CommonClass.tmHub.Publish(new MyMessage3(this, veea));

            return new ValidationResult(false, str);  // This line does not trigger Validation.ErrorEvent like the SpeedValidationRule and TimeValidationRule "Validation" methods
          }
        }
      }

      // Default to everything OK.
      return new ValidationResult(true, null);
    }
  }

  static class Extensions
  {
    public static void Sort<T>(this ObservableCollection<T> collection) where T : IComparable
    {
      List<T> sorted = collection.OrderBy(x => x).ToList();
      for (int i = 0; i < sorted.Count(); i++)
        collection.Move(collection.IndexOf(sorted[i]), i);
    }
  }
  public class MyMessage1 : GenericTinyMessage<InvalidAccelEventArgs>
  {
    public MyMessage1(object sender, InvalidAccelEventArgs content)
      : base(sender, content)
    { }
  }

  public class MyMessage2 : GenericTinyMessage<String>
  {
    public MyMessage2(object sender, String content)
      : base(sender, content)
    { }
  }

  public class MyMessage3 : GenericTinyMessage<ValidationErrorEventArgs>
  {
    public MyMessage3(object sender, ValidationErrorEventArgs content)
      : base(sender, content)
    { }
  }

}