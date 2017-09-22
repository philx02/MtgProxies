using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace MpcImageFormater
{
  public partial class Form1 : Form
  {
    private List<string> mUrls = new List<string>();
    private List<CardsInfo> mCardsInfoList = new List<CardsInfo>();

    public Form1()
    {
      InitializeComponent();
    }

    private void button1_Click(object sender, EventArgs e)
    {
      if (!File.Exists(textBox1.Text))
      {
        folderBrowserDialog1.ShowDialog();
        textBox1.Text = folderBrowserDialog1.SelectedPath;
      }
      foreach (var wCardsInfo in mCardsInfoList)
      {
        using (Image newBackground = new Bitmap(MpcImageFormater.Properties.Resources.background))
        {
          var wDest = Graphics.FromImage(newBackground);
          wDest.DrawImage(wCardsInfo.mPicBox.Image, 21, 21);
          newBackground.Save(textBox1.Text + @"\" + wCardsInfo.Name + ".bmp", ImageFormat.Bmp);
        }
      }
    }

    public class AlternateImage
    {
      public string Name { get; set; }
      public Image Value { get; set; }
    }

    private class CardsInfo
    {
      private string mName;
      public CardsInfo(string iName)
      {
        mName = iName;
      }
      public string Name { get { return mName; } }
      public PictureBox mPicBox = new PictureBox();
      public List<AlternateImage> mAlternateImages = new List<AlternateImage>();
    }

    private void button2_Click(object sender, EventArgs e)
    {
      var wForm2 = new Form2();
      wForm2.ShowDialog();
      if (!wForm2.Valid)
      {
        return;
      }
      panel1.Controls.Clear();
      mCardsInfoList.Clear();
      var wCardList = wForm2.CardList.Split(new string[]{"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
      var proxy = new WebProxy("proxy.cae.com", 8080);
      proxy.UseDefaultCredentials = true;
      var client = new WebClient();
      client.Proxy = proxy;
      string wCardsNotFound = string.Empty;
      foreach (var wCardName in wCardList)
      {
        string wMtgCardInfo = client.DownloadString("https://magiccards.info/query?q=%2B%2Bo%21%22" + WebUtility.UrlEncode(wCardName) + "%22&v=scan");
        var wRegex = new Regex("/scans/en/.*/.*.jpg");
        var wCardsInfo = new CardsInfo(wCardName);
        bool wCardFound = false;
        foreach (Match wMatch in wRegex.Matches(wMtgCardInfo))
        {
          wCardFound = true;
          var wUrl = "https://magiccards.info" + wMatch.Value;
          try
          {
            var stream = client.OpenRead(wUrl);
            var wImage = new Bitmap(stream);
            if (wImage.Width != 312)
            {
              continue;
            }
            wCardsInfo.mAlternateImages.Add(new AlternateImage() { Name = wMatch.Value.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries)[2], Value = wImage });
            if (wCardsInfo.mAlternateImages.Count == 1)
            {
              wCardsInfo.mPicBox.Image = wImage;
              wCardsInfo.mPicBox.Size = wCardsInfo.mPicBox.Image.Size;
            }
          }
          catch (WebException)
          {
          }
        }
        if (!wCardFound)
        {
          wCardsNotFound += wCardName + "\r\n";
          continue;
        }
        mCardsInfoList.Add(wCardsInfo);
        var wSelector = new ComboBox();
        wSelector.DataSource = wCardsInfo.mAlternateImages;
        wSelector.Left = 230;
        wSelector.Top = 50;
        wSelector.Width = 70;
        wSelector.DisplayMember = "Name";
        wSelector.ValueMember = "Value";
        wSelector.SelectedIndexChanged += new System.EventHandler(this.SelectionChange);
        wCardsInfo.mPicBox.Controls.Add(wSelector);
      }
      if (wCardsNotFound != string.Empty)
      {
        MessageBox.Show(wCardsNotFound, "Cards not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
      }
      if (mCardsInfoList.Count > 0)
      {
        button1.Enabled = true;
        RefreshCardList();
      }
    }

    private void SelectionChange(object sender, EventArgs e)
    {
      var wSelector = (ComboBox)sender;
      var wPicBox = (PictureBox)wSelector.Parent;
      var wImage = (AlternateImage)wSelector.SelectedItem;
      wPicBox.Image = wImage.Value;
    }

    private void RefreshCardList()
    {
      int wCurrentWidth = 0;
      int wCurrentHeight = 0;
      foreach (var wCardsInfo in mCardsInfoList)
      {
        panel1.Controls.Add(wCardsInfo.mPicBox);
        wCardsInfo.mPicBox.Left = wCurrentWidth;
        wCardsInfo.mPicBox.Top = wCurrentHeight;
        wCurrentWidth += wCardsInfo.mPicBox.Width;
        if (wCurrentWidth + wCardsInfo.mPicBox.Width >= panel1.Width)
        {
          wCurrentWidth = 0;
          wCurrentHeight += wCardsInfo.mPicBox.Height;
        }
      }
    }

    private void Form1_Resize(object sender, EventArgs e)
    {
      panel1.Width = this.Width - 40;
      panel1.Height = this.Height - 100;
      RefreshCardList();
    }

    private void button3_Click(object sender, EventArgs e)
    {
      var wForm3 = new Form3();
      wForm3.ShowDialog();
    }
  }
}
