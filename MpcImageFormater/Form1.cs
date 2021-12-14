using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace MpcImageFormater
{
  public partial class Form1 : Form
  {
    private Dictionary<string, CardInfo> mCardsInfoList = new Dictionary<string, CardInfo>();
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
          Rectangle blackRect = new Rectangle(500, 400, 200, 50);
          saveImage(wBorder, wTopAdjustment, wBottomAdjustment, cropRect, blackRect, wCardsInfo.Value.mSelectedImage, wCardsInfo.Value.Name);
        }
        else if (wCardsInfo.Value.mNewLegendaryBorders)
        {
          var wBorder = 54;
          var wTopAdjustment = 8;
          var wBottomAdjustment = 16;
          Rectangle cropRect = new Rectangle(20, 18, 706, 992);
          Rectangle blackRect = new Rectangle(460, wCardsInfo.Value.mBottomStyle == CardInfo.BottomStyle.Narrow ? 1035 : 1010, 300, wCardsInfo.Value.mBottomStyle == CardInfo.BottomStyle.Narrow ? 18 : 28);
          saveImage(wBorder, wTopAdjustment, wBottomAdjustment, cropRect, blackRect, wCardsInfo.Value.mSelectedImage, wCardsInfo.Value.Name);
        }
        else
        {
          var wBorder = 62;
          var wTopAdjustment = 8;
          var wBottomAdjustment = 8;
          Rectangle cropRect = new Rectangle(28, 28, 690, 984);
          Rectangle blackRect = new Rectangle(460, wCardsInfo.Value.mBottomStyle == CardInfo.BottomStyle.Narrow ? 1025 : 1015, 290, wCardsInfo.Value.mBottomStyle == CardInfo.BottomStyle.Narrow ? 28 : 38);
          //Rectangle cropRect = new Rectangle(28, 28, 690, wCardsInfo.Value.mBottomStyle == CardInfo.BottomStyle.Narrow ? 961 : 942);
          saveImage(wBorder, wTopAdjustment, wBottomAdjustment, cropRect, blackRect, wCardsInfo.Value.mSelectedImage, wCardsInfo.Value.Name);
        }
      }
    }

    private void saveImage(int wBorder, int wTopAdjustment, int wBottomAdjustment, Rectangle cropRect, Rectangle blackRect, Image iImage, string iName)
    {
      Bitmap target = new Bitmap(cropRect.Width + wBorder * 2, cropRect.Height + wBorder * 2 + wTopAdjustment + wBottomAdjustment);

      using (Graphics g = Graphics.FromImage(target))
      {
        g.DrawImage(iImage, new Rectangle(wBorder, wBorder + wTopAdjustment, cropRect.Width, cropRect.Height), cropRect, GraphicsUnit.Pixel);
        var pixel = target.GetPixel(450, 1035);
        var brush = new SolidBrush(pixel);
        g.FillRectangle(brush, blackRect);
        target.Save(textBox1.Text + @"\" + TransformToFileName(iName) + ".bmp", ImageFormat.Bmp);
      }
    }

    public class AlternateImage
    {
      public string Name { get; set; }
      public SelectionCard Value { get; set; }
    }

    public class CardInfo
    {
      private string mName;
      public CardInfo(string iName, BottomStyle iBottomStyle, bool iNewLegendaryBorder)
      {
        mName = iName;
        mBottomStyle = iBottomStyle;
        mNewLegendaryBorders = iNewLegendaryBorder;
      }
      public string Name { get { return mName; } }
      public PictureBox mPicBox = new PictureBox();
      public Image mSelectedImage;
      public List<AlternateImage> mAlternateImages = new List<AlternateImage>();
      public enum BottomStyle { Narrow, Wide };
      public BottomStyle mBottomStyle;
      public bool mNewLegendaryBorders;
    }

    public class SelectionCard
    {
      public SelectionCard(CardInfo iCardsInfo, Image iImage)
      {
        mCardsInfoRef = iCardsInfo;
        mImage = iImage;
      }
      public CardInfo mCardsInfoRef;
      public Image mImage;
    }

    public class ImageUris
    {
      public string png;
    }

    public class CardFaces
    {
      public string name;
      public string type_line;
      public ImageUris image_uris;
    }

    public class BulkData
    {
      public string download_uri { get; set; }
    }

    public class CardJson
    {
      public string name;
      public string set;
      public string set_name;
      public string layout;
      public string type_line;
      public ImageUris image_uris;
      public CardFaces[] card_faces;
    }

    private void AddCard(WebClient client, string png_url, string wCardName, string set_name, CardInfo.BottomStyle bottom_style, bool iNewLegendaryBorder)
    {
      var stream = client.OpenRead(png_url);
      var wImage = new Bitmap(stream);
      CardInfo wCardsInfo;
      if (!mCardsInfoList.TryGetValue(wCardName, out wCardsInfo))
      {
        wCardsInfo = new CardInfo(wCardName, bottom_style, iNewLegendaryBorder);
        var wSelector = new ComboBox();
        wSelector.DataSource = wCardsInfo.mAlternateImages;
        wSelector.Left = 130;
        wSelector.Top = 50;
        wSelector.Width = 170;
        wSelector.DisplayMember = "Name";
        wSelector.ValueMember = "Value";
        wSelector.SelectedIndexChanged += new System.EventHandler(this.SelectionChange);
        wCardsInfo.mPicBox.Controls.Add(wSelector);
        mCardsInfoList.Add(wCardName, wCardsInfo);
      }
      wCardsInfo.mAlternateImages.Add(new AlternateImage() { Name = set_name, Value = new SelectionCard(wCardsInfo, wImage) });
      if (wCardsInfo.mAlternateImages.Count == 1)
      {
        wCardsInfo.mPicBox.Image = (Image)(new Bitmap(wImage, new Size(312, 445)));
        wCardsInfo.mPicBox.Size = wCardsInfo.mPicBox.Image.Size;
        wCardsInfo.mSelectedImage = wImage;
      }
    }

    private async void button2_Click(object sender, EventArgs e)
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
      var wCardsNotFound = new List<string>(wCardsToFind);

      ServicePointManager.Expect100Continue = true;
      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

      var wBulkData = JsonConvert.DeserializeObject<BulkData>(client.DownloadString("https://api.scryfall.com/bulk-data/default-cards"));
      var wFilename = (new Uri(wBulkData.download_uri)).Segments.Last();
      var wCacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MpcImageFormater");
      Directory.CreateDirectory(wCacheFolder);
      var wCacheFile = Path.Combine(wCacheFolder, (new Uri(wBulkData.download_uri)).Segments.Last());
      if (!File.Exists(wCacheFile))
      {
        Directory.EnumerateDirectories(wCacheFolder).ToList().ForEach(x => Directory.Delete(x, true));
        Directory.EnumerateFiles(wCacheFolder).ToList().ForEach(x => File.Delete(x));
        await Task.Run(() => { client.DownloadFile(wBulkData.download_uri, wCacheFile); });
      }
      var wCardDb = await Task.Run(() => { return JsonConvert.DeserializeObject<CardJson[]>(File.ReadAllText(wCacheFile)); });

      foreach (var wCardName in wCardsToFind.Distinct())
      {
        try
        {
          foreach (var wCardJson in wCardDb.Where(x => x.name == wCardName || (x.card_faces != null && x.card_faces.First().name == wCardName)))
          {
            wCardsNotFound.Remove(wCardName);
            if (wCardJson.image_uris == null)
            {
              if (wCardJson.card_faces.Length == 2)
              {
                if (wCardJson.card_faces[0].image_uris == null)
                {
                  continue;
                }
                var wBottomStyle = wCardJson.card_faces[0].type_line.Contains("Creature") || wCardJson.card_faces[0].type_line.Contains("Planeswalker") ? CardInfo.BottomStyle.Narrow : CardInfo.BottomStyle.Wide;
                var wNewLegendaryBorder = wCardJson.card_faces[0].type_line.Contains("Legendary");
                AddCard(client, wCardJson.card_faces[0].image_uris.png, wCardJson.card_faces[0].name, wCardJson.set_name, wBottomStyle, wNewLegendaryBorder);
                wBottomStyle = wCardJson.card_faces[1].type_line.Contains("Creature") || wCardJson.card_faces[1].type_line.Contains("Planeswalker") ? CardInfo.BottomStyle.Narrow : CardInfo.BottomStyle.Wide;
                wNewLegendaryBorder = wCardJson.card_faces[0].type_line.Contains("Legendary");
                AddCard(client, wCardJson.card_faces[1].image_uris.png, wCardJson.card_faces[1].name, wCardJson.set_name, wBottomStyle, wNewLegendaryBorder);
              }
            }
            else
            {
              var wBottomStyle = wCardJson.layout != "split" && (wCardJson.type_line.Contains("Creature") || wCardJson.type_line.Contains("Planeswalker")) ? CardInfo.BottomStyle.Narrow : CardInfo.BottomStyle.Wide;
              var wNewLegendaryBorder = wCardJson.type_line.Contains("Legendary");
              AddCard(client, wCardJson.image_uris.png, wCardName, wCardJson.set_name, wBottomStyle, wNewLegendaryBorder);
            }
          }
        }
        catch (WebException exc)
        {
          Console.WriteLine("For card " + wCardName + ": " + exc.Message);
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
