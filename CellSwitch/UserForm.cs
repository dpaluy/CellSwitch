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
                DataRow user = ds_.Tables[0].NewRow();
                user["FirstName"] = txtFirstName.Text;
                user["LastName"] = txtLastName.Text;
                user["PhoneNumber"] = txtPhone.Text;
                user["Note"] = txtNote.Text;
                user["Enabled"] = true;
                ds_.Tables[0].Rows.Add(user);
            } catch (Exception err)
            {
                FormTools.ErrBox(err.ToString(), "Add new user");
            } finally {
                this.Close();
            }

        }
    }
}