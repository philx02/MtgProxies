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

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

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
        if (wCardsInfo.Value.mSelectedImage.Width == 672)
        {
          var wBorder = 60;
          var wTopAdjustment = 6;
          var wBottomAdjustment = 6;
          Rectangle cropRect = new Rectangle(24, 24, 624, 888);
          saveImage(wBorder, wTopAdjustment, wBottomAdjustment, cropRect, wCardsInfo.Value.mSelectedImage, wCardsInfo.Value.Name);
        }
        else
        {
          var wBorder = 62;
          var wTopAdjustment = 8;
          var wBottomAdjustment = 8;
          Rectangle cropRect = new Rectangle(28, 28, 690, 984);
          saveImage(wBorder, wTopAdjustment, wBottomAdjustment, cropRect, wCardsInfo.Value.mSelectedImage, wCardsInfo.Value.Name);
        }
      }
    }

    private void saveImage(int wBorder, int wTopAdjustment, int wBottomAdjustment, Rectangle cropRect, Image iImage, string iName)
    {
      Bitmap target = new Bitmap(cropRect.Width + wBorder * 2, cropRect.Height + wBorder * 2 + wTopAdjustment + wBottomAdjustment);

      using (Graphics g = Graphics.FromImage(target))
      {
        g.DrawImage(iImage, new Rectangle(wBorder, wBorder + wTopAdjustment, cropRect.Width, cropRect.Height), cropRect, GraphicsUnit.Pixel);
        target.Save(textBox1.Text + @"\" + TransformToFileName(iName) + ".bmp", ImageFormat.Bmp);
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

    public class CardJson
    {
      public string name;
      public string set;
      public string setName;
      public string number;
      public string layout;
    }

    public class CardJsonList
    {
      public CardJson[] cards;
    }

    private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
    {
      // If the certificate is a valid, signed certificate, return true.
      if (error == System.Net.Security.SslPolicyErrors.None)
      {
        return true;
      }

      Console.WriteLine("X509Certificate [{0}] Policy Error: '{1}'",
          cert.Subject,
          error.ToString());

      return false;
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
      var wCardsToFind = new List<string>(wCleanedList.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries));
      var wCardsNotFound = wCardsToFind;

      ServicePointManager.Expect100Continue = true;
      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

      var wString = client.DownloadString("https://api.magicthegathering.io/v1/cards?name=" + wCleanedList);
      var wCardJsonList = Newtonsoft.Json.JsonConvert.DeserializeObject<CardJsonList>(wString);
      
      if (wCardJsonList.cards.Count() != 0)
      {
        foreach (var wCard in wCardJsonList.cards)
        {
          if (wCard.set == string.Empty || wCard.number == string.Empty || !wCardsToFind.Contains(wCard.name))
          {
            continue;
          }
          wCardsNotFound.Remove(wCard.name);
          var wCardNumber = wCard.number;
          if (wCard.layout == "aftermath")
          {
            wCardNumber = wCardNumber.TrimEnd(new char[] { 'a', 'b' });
          }
          var wUrl = "https://img.scryfall.com/cards/png/en/" + wCard.set.ToLower() + "/" + wCardNumber + ".png";
          try
          {
            var stream = client.OpenRead(wUrl);
            var wImage = new Bitmap(stream);
            CardsInfo wCardsInfo;
            if (!mCardsInfoList.TryGetValue(wCard.name, out wCardsInfo))
            {
              wCardsInfo = new CardsInfo(wCard.name);
              var wSelector = new ComboBox();
              wSelector.DataSource = wCardsInfo.mAlternateImages;
              wSelector.Left = 130;
              wSelector.Top = 50;
              wSelector.Width = 170;
              wSelector.DisplayMember = "Name";
              wSelector.ValueMember = "Value";
              wSelector.SelectedIndexChanged += new System.EventHandler(this.SelectionChange);
              wCardsInfo.mPicBox.Controls.Add(wSelector);
              mCardsInfoList.Add(wCard.name, wCardsInfo);
            }
            wCardsInfo.mAlternateImages.Add(new AlternateImage() { Name = wCard.setName, Value = new SelectionCard(wCardsInfo, wImage) });
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
      if (wCardsNotFound.Count != 0)
      {
        string wCardsNotFoundString = string.Empty;
        foreach (var wCardNotFound in wCardsNotFound)
        {
          wCardsNotFoundString += wCardNotFound + "\r\n";
        }
        MessageBox.Show(wCardsNotFoundString, "Cards not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
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
