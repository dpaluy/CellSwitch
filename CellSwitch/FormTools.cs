using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace CellSwitch
{
    public sealed class FormTools
    {
        public const int SEC = 1000; // sec definition by msec

        #region Open Dialog
        public static string openFileDialog(string filter, string title)
        {
            OpenFileDialog ofn = new OpenFileDialog();
            ofn.Filter = filter;
            ofn.Title = title;
            while (true)
            {
                switch (ofn.ShowDialog())
                {
                    case DialogResult.Cancel:
                        return "";
                    case DialogResult.OK:
                        return ofn.FileName;
                    default:
                        break;
                }
            }
        }
        #endregion

        #region Save Dialog
        public static string saveFileDialog(string filter, string title)
        {
            SaveFileDialog ofn = new SaveFileDialog();
            ofn.Filter = filter;
            ofn.Title = title;
            while (true)
            {
                switch (ofn.ShowDialog())
                {
                    case DialogResult.Cancel:
                        return "";
                    case DialogResult.OK:
                        return ofn.FileName;
                    default:
                        break;
                }
            }
        }   
        #endregion

        #region Error Message Box
        public static void ErrBox(string msg, string caption)
        {
            MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        #endregion

        #region Confirmation Message Box
        public static DialogResult ConfimBox(string msg, string caption)
        {
            return MessageBox.Show(msg, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }
        #endregion

        #region Info Message Box
        public static void InfoBox(string msg, string caption)
        {
            MessageBox.Show(msg, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        #region Wait
        public static void Wait(int sleep)
        {
            int loop = (int)(sleep/100);
            for (int i = 0; i < loop; i++)
            {
                System.Threading.Thread.Sleep(100);
                Application.DoEvents();
            }
        }

        public static void WaitOnCondition(bool condition, int max_tries)
        {
            int tries = 0;
            while (!condition && tries < max_tries)
            {
                Wait(SEC);
                ++tries;
            }
        }
        #endregion
    }

    #region AutoCompleteComboBox
    //public class AutoCompleteComboBox : System.Windows.Forms.ComboBox
    //{
    //    public event System.ComponentModel.CancelEventHandler NotInList;

    //    private bool _limitToList = true;
    //    private bool _inEditMode = false;

    //    public AutoCompleteComboBox()
    //        : base()
    //    {
    //    }

    //    [Category("Behavior")]
    //    public bool LimitToList
    //    {
    //        get { return _limitToList; }
    //        set { _limitToList = value; }
    //    }

    //    protected virtual void OnNotInList(System.ComponentModel.CancelEventArgs e)
    //    {
    //        if (NotInList != null)
    //        {
    //            NotInList(this, e);
    //        }
    //    }

    //    protected override void OnTextChanged(System.EventArgs e)
    //    {
    //        if (_inEditMode)
    //        {
    //            string input = Text;
    //            int index = FindString(input);

    //            if (index >= 0)
    //            {
    //                _inEditMode = false;
    //                SelectedIndex = index;
    //                _inEditMode = true;
    //                Select(input.Length, Text.Length);
    //            }
    //        }

    //        base.OnTextChanged(e);
    //    }

    //    protected override void OnValidating(System.ComponentModel.CancelEventArgs e)
    //    {
    //        if (this.LimitToList)
    //        {
    //            int pos = this.FindStringExact(this.Text);

    //            if (pos == -1)
    //            {
    //                OnNotInList(e);
    //            }
    //            else
    //            {
    //                this.SelectedIndex = pos;
    //            }
    //        }

    //        base.OnValidating(e);
    //    }

    //    protected override void OnKeyDown(System.Windows.Forms.KeyEventArgs e)
    //    {
    //        _inEditMode = (e.KeyCode != Keys.Back && e.KeyCode != Keys.Delete);
    //        base.OnKeyDown(e);
    //    }
    //}
    #endregion

}
