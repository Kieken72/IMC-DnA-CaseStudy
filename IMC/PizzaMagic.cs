using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace IMC
{
    public class PizzaMagic
    {   
        public static void ShowHome()
        {
            Console.Clear();


            XmlSerializer ser = new XmlSerializer(typeof(BestellingenKaft));
            BestellingenKaft aa;
            using (FileStream myFileStream = new FileStream("Bestellingen.xml", FileMode.OpenOrCreate))
            {
                aa = (BestellingenKaft) ser.Deserialize(myFileStream);
            }


            Console.WriteLine(File.ReadAllText("Logo.txt"));
            Console.WriteLine("");
            Console.WriteLine("Benvenuti nella Pizzeria El Brambo!");
            Console.WriteLine("");

            Console.WriteLine("Bestellingen:");
            foreach (var bestelling in aa.Bestellingen)
            {
                Console.WriteLine("- " + bestelling.BestelNummer + ": " + bestelling.KlantNaam + "\t" + bestelling.Status);
            }

            Console.Write("Typ bestellingnummer voor detail of <enter> voor nieuwe invoer: ");

            var input = Console.ReadLine();

            var gevondenBestelling = aa.Bestellingen.SingleOrDefault(b => b.BestelNummer == input);

            if (gevondenBestelling != null)
            {
                ToonBestaandeBestelling(gevondenBestelling);
            }
            else
            {

                ToonNieuweBestelling();
            }
        }

        private static void ToonNieuweBestelling(Bestelling theOrder = null)
        {
            theOrder = theOrder ?? new Bestelling();

            Console.WriteLine("Invoer van bestelling.");

            Console.Write("Klantnaam: ");
            theOrder.KlantNaam = Console.ReadLine();

            Console.Write("Adres: ");
            theOrder.KlantAdres = Console.ReadLine();

            ToonArtikelToevoegen(theOrder);
        }

        private static void ToonArtikelToevoegen(Bestelling theOrder)
        {
            var artikelData = JsonConvert.DeserializeObject<List<Artikel>>(File.ReadAllText("articles.json"));

            // Toon lijst bestellinglijnen
            Console.WriteLine("Items:");
            foreach (var theOrderItem in theOrder.Items)
            {
                Console.WriteLine($"\t{theOrderItem.Artikel.ArticleNumber}\t{theOrderItem.Artikel.Naam}\t{theOrderItem.Artikel.SalesPrice}\t AANTAL: {theOrderItem.Aantal}");
            }
            Console.WriteLine("Totaal: " + theOrder.TotalePrijs + " EURO");

            Console.WriteLine("< MENUKAART >");

            var theMenuItems = new Dictionary<string, Action>();

            foreach (var artikel in artikelData)
            {
                theMenuItems.Add($"{artikel.ArticleNumber}\t{artikel.Naam}" + "\t" + artikel.SalesPrice,
                    () => AddItem(theOrder, artikel));
            }

            theMenuItems.Add("Plaats order", () => ConfirmationOrdre(theOrder));
            theMenuItems.Add("Back to main menu", () => ShowHome());

            MenuLoopHelper.ShowMenuLoop(theMenuItems);
        }

        private static void ConfirmationOrdre(Bestelling theOrder)
        {
            if (theOrder.Items.Where(i => i.Artikel.ArticleNumber == "k1" || i.Artikel.ArticleNumber == "k2" || i.Artikel.ArticleNumber == "k3")
                    .Count() > 1)
            {
                throw new ArgumentException("Nein! Slechts 1 type korting per bestelling toegelaten");
            } 

            XmlSerializer ser = new XmlSerializer(typeof(BestellingenKaft));

            BestellingenKaft aa;
            using (FileStream myFileStream = new FileStream("Bestellingen.xml", FileMode.OpenOrCreate))
            {
                aa = (BestellingenKaft)ser.Deserialize(myFileStream);
            }

            theOrder.BestelNummer = (aa.Bestellingen.Count + 1).ToString("D4");
            theOrder.BestelDatum = DateTime.Now;

            aa.Bestellingen.Add(theOrder);

            using (FileStream myFileStream = new FileStream("Bestellingen.xml", FileMode.OpenOrCreate))
            {
                ser.Serialize(myFileStream, aa);
            }        
            
            ShowHome();
        }

        private static void AddItem(Bestelling theOrder, Artikel artikel)
        {
            // kijk of er al een lijn is

            var aLine = theOrder.Items.FirstOrDefault(i => i.Artikel.ArticleNumber == artikel.ArticleNumber);

            if (aLine != null)
            {
                aLine.Aantal++;
            }
            else
            {
                theOrder.Items.Add(new BestellingLijn
                {
                    Artikel = artikel,
                    Aantal = 1
                });
            }
                        

            // Calculate price
            theOrder.TotalePrijs += artikel.SalesPrice;

            Console.WriteLine("Item added!");

            ToonArtikelToevoegen(theOrder);
        }

        private static void ToonBestaandeBestelling(Bestelling gevondenBestelling)
        {
            Console.Clear();

            Console.WriteLine("Bestelling: " + gevondenBestelling.BestelNummer);
            Console.WriteLine("Klant: " + gevondenBestelling.KlantNaam);


            MenuLoopHelper.ShowMenuLoop(new Dictionary<string, Action>
            {
                
            });

            Console.WriteLine("----");
            Console.WriteLine("1) Wijzig status");
            Console.WriteLine("2) Voeg betalingsinfo toe");

            Console.ReadLine();
        }

        public class Artikel
        {
            [JsonProperty(PropertyName = "artikelnr")]
            public string ArticleNumber { get; set; }

            [JsonProperty(PropertyName = "name")]
            public string Naam { get; set; }

            [JsonProperty(PropertyName = "prixdevente")]
            public decimal SalesPrice { get; set; }

            [JsonProperty(PropertyName = "prixdachat")]
            public decimal PurchasePrice { get; set; }
        }

        public class BestellingenKaft
        {
            public List<Bestelling> Bestellingen { get; set; }
        }

        public class Bestelling
        {
            public string BestelNummer { get; set; }

            public string KlantNaam { get; set; }
            public string KlantAdres { get; set; }

            public DateTime BestelDatum { get; set; }

            public List<BestellingLijn> Items { get; set; } = new List<BestellingLijn>();

            public BestellingType Type { get; set; }
            public BestellingStatus Status { get; set; }

            public bool IsBetaald { get; set; }

            // TODO: Implement payment info
            public bool IsCashPayment { get; set; }
            public DateTime DatePaid { get; set; }
            public string PaymentReference { get; set; }

            public decimal TotalePrijs { get; set; }
        }

        public class BestellingLijn
        {
            public Artikel Artikel { get; set; }

            public int Aantal { get; set; }
        }

        public enum BestellingType
        {
            TelefonischAfhaal,
            TelefonischBezorg,
            InDeZaak           
        }

        public class MenuLoopHelper
        {
            public static void ShowMenuLoop(IDictionary<string, Action> items)
            {
                Console.WriteLine("Please make a choice");
                int n = 1;
                foreach (var action in items)
                {
                    Console.WriteLine($"\t{n++}\t{action.Key}");
                }
                Console.Write("Your choice: ");

                var choice = Console.ReadLine();

                if (int.TryParse(choice, out int choiceInt))
                {
                    if (choiceInt > items.Count || choiceInt < 1)
                    {
                        // try again
                        ShowMenuLoop(items);
                    }
                    else
                    {
                        var theAction = items.ElementAt(choiceInt - 1);

                        theAction.Value.Invoke();
                    }
                }
                else
                {
                    // try again
                    ShowMenuLoop(items);
                }
            }
        }

        public enum BestellingStatus
        {
            Wachtend,
            Keuken,
            LigtKlaar,
            BezorgerOnderweg,
            Bezorgd,
            Afgehaald,
            Geserveerd
        }
      
    }
}