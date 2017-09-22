using System;
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
  public partial class Form2 : Form
  {
    public Form2()
    {
      InitializeComponent();
    }

    private bool mValid = false;

    private void button1_Click(object sender, EventArgs e)
    {
      mValid = true;
      this.Hide();
    }

    public string CardList
    {
      get { return textBox1.Text; }
    }

    public bool Valid
    {
      get { return mValid; }
    }

    private void button2_Click(object sender, EventArgs e)
    {
      mValid = false;
      this.Hide();
    }
  }
}
