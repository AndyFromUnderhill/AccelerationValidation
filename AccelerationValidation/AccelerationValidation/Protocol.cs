using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccelerationValidation
{
  public class CProtocol : INotifyPropertyChanged
  {
    public static int MAX_PROTOCOL_SECONDS = 3600; // 1 hours * 60 min/hr * 60 sec/min

    public CProtocol()
    {
      Name = "Untitled Protocol";
      Transitions = new ObservableCollection<CTransition>();
    }
    public CProtocol(string protocolName)
    {
      Name = protocolName;
      Transitions = new ObservableCollection<CTransition>();
    }

    private string name;
    public string Name
    {
      get { return name; }
      set
      {
        if (name != value)
        {
          name = value;
          NotifyPropertyChanged("Name");
        }
      }
    }


    private TimeSpan duration;
    //[XmlIgnore]
    public TimeSpan Duration
    {
      get { return duration; }
      set
      {
        if (duration != value)
        {
          duration = value;
          NotifyPropertyChanged("Duration");
        }
      }
    }

    public double DurationSeconds
    {
      get { return duration.TotalSeconds; }
      set { duration = new TimeSpan(0, 0, (int)value); }
    }


    private int loadspeed;
    public int LoadSpeed
    {
      get { return loadspeed; }
      set
      {
        if (loadspeed != value)
        {
          loadspeed = value;
          NotifyPropertyChanged("LoadSpeed");
        }
      }
    }

    private ObservableCollection<CTransition> transitions;
    public ObservableCollection<CTransition> Transitions
    {
      get { return transitions; }
      set
      {
        if (transitions != value)
        {
          transitions = value;
          NotifyPropertyChanged("Transitions");
        }
      }
    }

    public void AddTransition(CTransition transition)
    {
      transitions.Add(transition);
      CTransition.SetValidationTransitionList(Transitions);
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

    internal void DeleteTransition(CTransition selectedItem)
    {
      try 
      { 
        transitions.Remove(selectedItem);
        CTransition.SetValidationTransitionList(Transitions);
      }
      catch(Exception e)
      {
        System.Diagnostics.Debug.WriteLine(e.Message);
      }
    }
    #endregion

  }
}
