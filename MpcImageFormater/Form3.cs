﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MpcImageFormater
{
  public partial class Form3 : Form
  {
    public Form3()
    {
      InitializeComponent();
    }

    private bool mComfirmed = false;

    public bool Comfirmed
    {
      get { return mComfirmed; }
    }

    private void button1_Click(object sender, EventArgs e)
    {
      mComfirmed = true;
      this.Hide();
    }

    private void checkBox1_CheckedChanged(object sender, EventArgs e)
    {
      textBox3.Enabled = !checkBox1.Checked;
      textBox4.Enabled = !checkBox1.Checked;
    }

    private void button2_Click(object sender, EventArgs e)
    {
      mComfirmed = false;
      this.Hide();
    }

    public string Hostname
    {
      get { return textBox1.Text; }
    }

    public int Port
    {
      get { return Convert.ToInt32(textBox2.Text); }
    }

    public string Login
    {
      get { return textBox3.Text; }
    }

    public string Password
    {
      get { return textBox4.Text; }
    }
  }
}
