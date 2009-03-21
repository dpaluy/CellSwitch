using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace CellSwitch
{
    public partial class UserForm : Form
    {
        private DataSet ds_;

        public UserForm(DataSet ds)
        {
            InitializeComponent();
            this.ds_ = ds;
            this.StartPosition = FormStartPosition.CenterScreen;

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            // check required
            if (txtPhone.Text == string.Empty)
            {
                msgPhone.Text = "Required";
                msgPhone.Visible = true;
                txtPhone.Focus();
                return;
            }
            try {
                if (!addNewUser(txtFirstName.Text, txtLastName.Text, txtPhone.Text, txtNote.Text))
                    FormTools.ErrBox("The Phone Number already exists!", "Add new user");
                else
                    this.Close();

            } catch (Exception err)
            {
                FormTools.ErrBox(err.Message, "Add new user");
                this.Close();
            }
        }

        private bool addNewUser(string firstName, string lastName, string phone, string note)
        {
            DataRow user = ds_.Tables[0].NewRow();
            user["FirstName"] = firstName;
            user["LastName"] = lastName;
            user["PhoneNumber"] = phone;
            user["Note"] = note;
            user["Enabled"] = true;
            foreach (DataRow row in ds_.Tables[0].Rows)
            {
                string currentPhone = (string)row["PhoneNumber"];
                if (currentPhone.CompareTo(phone) == 0)
                {
                    user = null;
                    break;
                }
            }
            if (user != null)
            {
                ds_.Tables[0].Rows.Add(user);
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}