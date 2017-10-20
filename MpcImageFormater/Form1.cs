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
using MtgApiManager.Lib.Service;

namespace MpcImageFormater
{
  public partial class Form1 : Form
  {
    private List<string> mUrls = new List<string>();
    private Dictionary<string, CardsInfo> mCardsInfoList = new Dictionary<string, CardsInfo>();
    private WebProxy mProxy;
    private Form3 mForm3 = new Form3();
    private Form2 mForm2 = new Form2();

    public Form1()
    {
      InitializeComponent();
    }

    private string TransformToFileName(string iInput)
    {
      char[] wInvalidPathChars = new char[] { '<', '>', ':', '\"', '/', '\\', '|', '?', '*' };
      foreach (var wInvalidChar in wInvalidPathChars)
      {
        iInput = iInput.Replace(wInvalidChar, '_');
      }
      return iInput;
    }

    private void button1_Click(object sender, EventArgs e)
    {
      if (!Directory.Exists(textBox1.Text))
      {
        folderBrowserDialog1.ShowDialog();
        if (folderBrowserDialog1.SelectedPath == string.Empty)
        {
          return;
        }
        textBox1.Text = folderBrowserDialog1.SelectedPath;
      }
      foreach (var wCardsInfo in mCardsInfoList)
      {
        using (Image newBackground = new Bitmap(MpcImageFormater.Properties.Resources.background))
        {
          var wDest = Graphics.FromImage(newBackground);
          wDest.DrawImage(wCardsInfo.Value.mSelectedImage, 21, 21);
          newBackground.Save(textBox1.Text + @"\" + TransformToFileName(wCardsInfo.Value.Name) + ".bmp", ImageFormat.Bmp);
        }
      }
    }

    public class AlternateImage
    {
      public string Name { get; set; }
      public SelectionCard Value { get; set; }
    }

    public class CardsInfo
    {
      private string mName;
      public CardsInfo(string iName)
      {
        mName = iName;
      }
      public string Name { get { return mName; } }
      public PictureBox mPicBox = new PictureBox();
      public Image mSelectedImage;
      public List<AlternateImage> mAlternateImages = new List<AlternateImage>();
    }

    public class SelectionCard
    {
      public SelectionCard(CardsInfo iCardsInfo, Image iImage)
      {
        mCardsInfoRef = iCardsInfo;
        mImage = iImage;
      }
      public CardsInfo mCardsInfoRef;
      public Image mImage;
    }

    private void button2_Click(object sender, EventArgs e)
    {
      mForm2.ShowDialog();
      if (!mForm2.Valid)
      {
        return;
      }
      label1.Visible = true;
      this.Refresh();
      panel1.Controls.Clear();
      mCardsInfoList.Clear();
      var wCleanedList = mForm2.CardList.Replace("\r", string.Empty).Replace("\n", "|");
      var client = new WebClient();
      client.Proxy = mProxy;
      string wCardsNotFound = string.Empty;
      CardService wService = new CardService();
      var wResult = wService.Where(x => x.Name, wCleanedList).All();
      if (wResult.IsSuccess)
      {
        foreach (var wCard in wResult.Value)
        {
          if (wCard.Set == string.Empty || wCard.Number == string.Empty)
          {
            continue;
          }
          var wUrl = "https://img.scryfall.com/cards/large/en/" + wCard.Set.ToLower() + "/" + wCard.Number + ".jpg";
          try
          {
            var stream = client.OpenRead(wUrl);
            var wImage = new Bitmap(stream);
            CardsInfo wCardsInfo;
            if (!mCardsInfoList.TryGetValue(wCard.Name, out wCardsInfo))
            {
              wCardsInfo = new CardsInfo(wCard.Name);
              var wSelector = new ComboBox();
              wSelector.DataSource = wCardsInfo.mAlternateImages;
              wSelector.Left = 130;
              wSelector.Top = 50;
              wSelector.Width = 170;
              wSelector.DisplayMember = "Name";
              wSelector.ValueMember = "Value";
              wSelector.SelectedIndexChanged += new System.EventHandler(this.SelectionChange);
              wCardsInfo.mPicBox.Controls.Add(wSelector);
              mCardsInfoList.Add(wCard.Name, wCardsInfo);
            }
            wCardsInfo.mAlternateImages.Add(new AlternateImage() { Name = wCard.SetName, Value = new SelectionCard(wCardsInfo, wImage) });
            if (wCardsInfo.mAlternateImages.Count == 1)
            {
              wCardsInfo.mPicBox.Image = (Image)(new Bitmap(wImage, new Size(312, 445)));
              wCardsInfo.mPicBox.Size = wCardsInfo.mPicBox.Image.Size;
              wCardsInfo.mSelectedImage = wImage;
            }
          }
          catch (WebException exc)
          {
            Console.WriteLine("For URL " + wUrl + ": " + exc.Message);
          }
        }
      }
      if (wCardsNotFound != string.Empty)
      {
        MessageBox.Show(wCardsNotFound, "Cards not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
      }
      label1.Visible = false;
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
      wPicBox.Image = (Image)(new Bitmap(wImage.Value.mImage, new Size(312, 445)));
      wImage.Value.mCardsInfoRef.mSelectedImage = wImage.Value.mImage;
    }

    private void RefreshCardList()
    {
      int wCurrentWidth = 0;
      int wCurrentHeight = 0;
      foreach (var wCardsInfo in mCardsInfoList)
      {
        panel1.Controls.Add(wCardsInfo.Value.mPicBox);
        wCardsInfo.Value.mPicBox.Left = wCurrentWidth;
        wCardsInfo.Value.mPicBox.Top = wCurrentHeight;
        wCurrentWidth += wCardsInfo.Value.mPicBox.Width;
        if (wCurrentWidth + wCardsInfo.Value.mPicBox.Width >= panel1.Width)
        {
          wCurrentWidth = 0;
          wCurrentHeight += wCardsInfo.Value.mPicBox.Height;
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
      mForm3.ShowDialog();
      if (mForm3.UsingProxy)
      {
        mProxy = new WebProxy(mForm3.Hostname, mForm3.Port);
        if (mForm3.UsingDefaultCredentials)
        {
          mProxy.UseDefaultCredentials = true;
        }
        else
        {
          mProxy.Credentials = new NetworkCredential(mForm3.Login, mForm3.Password);
        }
      }
      else
      {
        mProxy = null;
      }
    }
  }
}
