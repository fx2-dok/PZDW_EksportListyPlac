using Microsoft.Identity.Client;
using Soneta.Business;
using Soneta.Business.UI;
using Soneta.Kadry;
using Soneta.Place;
using Soneta.Samochodowka;
using Soneta.Towary;
using Soneta.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

[assembly: Worker(typeof(EksportListyPlac.ExportDoXml), typeof(ListaPlac))]

namespace EksportListyPlac
{
    public class ExportDoXml
    {
        public class Params : ContextBase
        {
            public Params(Context cx) : base(cx) { }

            private string rodzaj;

            [Soneta.Tools.Priority(1)]
            [Caption("Rodzaj listy płac")]
            //[Required]
            public string RodzajListy
            {
                get
                {
                    return this.rodzaj;
                }
                set
                {
                    this.rodzaj = value;
                    OnChanged();
                }
            }

            public object GetListRodzajListy()
            {
                Soneta.Business.View view = KadryModule.GetInstance(Session).Pracownicy.CreateView();
                return new[] {
                    "PŁACE",
                    "NAGRODY",
                    "NAGRODY JUBILEUSZOWE",
                    "DODATKOWE WYN. ROCZNE",
                    "ODPRAWA PIENIĘŻNA",
                    "ODSZKODOWANIE",
                    "ZFŚS",
                    "ZFŚS WCZASY",
                    "ZFŚS LECZENIE SANATORYJNE",
                    "ODPRAWA EMERYTALNA"
                };
            }

            private string abc;
            
            

        }
        static private Params param;

        [Context]
        public static Params Parametry //ustawienie parametrów
        {
            get { return param; }
            set { param = value; }
        }

        public int iteracje_globalne = 0;
        public int numer_pozycji = 0;
        decimal kwota_ma = 0;
        decimal kwota_wn = 0;
        StreamWriter sw = null;
        string path => Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        [Context]
        public ListaPlac[] listyPlac { get; set; }

        
        [Context]
        public Context Context { get; set; }
        [Action(
            "Eksport dekretu do pliku",
            Priority = 1,
            Icon = ActionIcon.Export,
            Mode = ActionMode.SingleSession,
            Target = ActionTarget.Menu | ActionTarget.ToolbarWithText)]

        public MessageBoxInformation Funkcja()
        {
            iteracje_globalne++;
            if (iteracje_globalne == 1) // jeżeli w kontekscie jest więcej niż 1 deklaracja (n) - program uruchamiał się n-razy
            {
                string rodzaj_deklaracji = param.RodzajListy;

                //SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                //saveFileDialog1.Filter = "XML files(.xml)|*.xml|all Files(*.*)|*.*";
                //saveFileDialog1.FilterIndex = 1;
                //saveFileDialog1.FileName = "dek_" + listyPlac[0].Data.Month /*+ Date.Today.Month.ToString() */ + "_" + rodzaj_deklaracji.ToLower().Replace(" ", "_");
                //saveFileDialog1.RestoreDirectory = true;

                //if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                //{
                List<Hashtable> listaHtWorker = new List<Hashtable>();
                List<Hashtable> listaHt = new List<Hashtable>();

                //zebranie informacji o wszstkich listach plac
                #region ODCZYTDANYCH
                List<ListaPlac> lP = new List<ListaPlac>();
                for (int l = 0; l < listyPlac.Length; l++)
                    lP.Add(listyPlac[l]);


                for (int i = 0; i < listyPlac.Length; i++)
                {
                    //KsiegaModule km = KsiegaModule.GetInstance(listyPlac[i].Session);
                    //OkresObrachunkowy okObrach = km.OkresyObrach[listyPlac[i].Data];

                    ListaPlac listaPlac = listyPlac[i];

                    string dataZapisu = "";
                    if (listaPlac.Okres.To.DayOfWeek == DayOfWeek.Sunday)
                        dataZapisu = (listaPlac.Okres.To - 2).ToString("yyMMdd");
                    else if (listaPlac.Okres.To.DayOfWeek == DayOfWeek.Saturday)
                        dataZapisu = (listaPlac.Okres.To - 1).ToString("yyMMdd");
                    else
                        dataZapisu = listaPlac.Okres.To.ToString("yyMMdd");


                    Hashtable ht = new Hashtable();
                    Hashtable htworker = new Hashtable();


                    foreach (Soneta.Place.Wyplata wyplata in listaPlac.Wyplaty)
                    {
                        string keyworker = wyplata.Pracownik.Kod;
                        Sumator.Sumator sumworker = (Sumator.Sumator)htworker[keyworker];
                        if (sumworker == null)
                        {
                            sumworker = new Sumator.Sumator(wyplata.Pracownik, listaPlac);
                            htworker.Add(keyworker, sumworker);
                        }
                        sumworker.addFromWorker(wyplata);


                        foreach (WypElement element in wyplata.Elementy)
                        {
                            string key = element.Nazwa;
                            Sumator.Sumator sum = (Sumator.Sumator)ht[key];
                            if (sum == null)
                            {
                                sum = new Sumator.Sumator(element.Pracownik, element.Definicja);
                                ht.Add(key, sum);
                            }
                            sum.Add(element);
                        }
                    }
                    listaHtWorker.Add(htworker);
                    listaHt.Add(ht);
                }
                #endregion
                sw = new StreamWriter(path +"/lista_plac_" + listyPlac[0].Data.Month.ToString() + ".txt", false, Encoding.UTF8);
                sw.WriteLine("Wydział Finansowy");
                sw.WriteLine("tel. +48 (85) 67 67 137");
                sw.WriteLine("faks +48 (85) 67 67 153");
                sw.WriteLine("Podlaski Zarząd Dróg Wojewódzkich w Białymstoku");
                sw.WriteLine("ul. Elewatorska 6, 15-620 Białystok");
                sw.WriteLine("www.pzdw.bialystok.pl" + Environment.NewLine);
                sw.WriteLine("Uruchomienie: " + Date.Now);
                sw.WriteLine("");
                sw.WriteLine(string.Format("{0,-17}", "Konto") + string.Format("{0,14}", "Kwota WN") + string.Format("{0,14}", "Kwota MA") + string.Format("{0,5}", "") + string.Format("{0,-28}", "Komentarz"));

                StreamWriter plik = new StreamWriter(path + "/lista_plac_" + listyPlac[0].Data.Month.ToString() +".xml", false, Encoding.GetEncoding("Windows-1250"));
                plik.WriteLine(@"<?xml version=""1.0"" encoding=""windows-1250""?>"); // polskie znaki

                string data = Date.Now.ToString("yyyy-MM-dd");

                XElement drzewo_start = new XElement("LISTA_PLAC",
                        new XElement("NUMER", "place " + Date.Now.Month.ToString() + "/" + Date.Now.Year.ToString()),
                        new XElement("DATA_SPORZADZENIA", data),
                        new XElement("SPOSOB_REALIZACJI", "3"));

                XElement pozycje_lp = new XElement("POZYCJE_LP");

                // START dzielenie na rodzaj deklaracji

                switch (rodzaj_deklaracji.ToUpper())
                {
                    case "NAGRODY": //##########################
                        wypisz_element_do_listy_xml(listaHt, lP, pozycje_lp, "Nagroda pieniężna", "231-00-1");
                        wypisz_podatki_do_listy_xml(listaHtWorker, lP, pozycje_lp);
                        wypisz_potracenie_komornik(listaHt, pozycje_lp);
                        wypisz_ppk_pracownik(listaHtWorker, pozycje_lp);
                        wypisz_ppk_pracodawca(listaHtWorker, pozycje_lp, lP);
                        break;

                    case "NAGRODY JUBILEUSZOWE": //##########################
                        wypisz_element_do_listy_xml(listaHt, lP, pozycje_lp, "Nagroda jubileusz", "231-00-1");
                        wypisz_podatek_do_us(listaHtWorker, pozycje_lp);
                        break;

                    case "DODATKOWE WYN. ROCZNE": //##########################
                        wypisz_element_do_listy_xml(listaHt, lP, pozycje_lp, "DodatkoweInformacje wynagrodzenie roczne", "231-00-1");
                        wypisz_potracenie_komornik(listaHt, pozycje_lp);
                        wypisz_ppk_pracownik(listaHtWorker, pozycje_lp);
                        break;

                    case "ODPRAWA PIENIĘŻNA": //##########################
                        wypisz_element_do_listy_xml(listaHt, lP, pozycje_lp, "Odprawa pieniężna", "231-00-1");
                        wypisz_podatek_do_us(listaHtWorker, pozycje_lp);
                        break;

                    case "ODSZKODOWANIE": //##########################
                        wypisz_element_do_listy_xml(listaHt, lP, pozycje_lp, "Odszkodowanie", "231-00-1");
                        wypisz_podatek_do_us(listaHtWorker, pozycje_lp);
                        break;

                    case "ZFŚS": //##########################
                        decimal kwota_listy_temp_1 = 0;
                        for (int i = 0; i < listaHt.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHt[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                if (sumatorworker.Definicja.ToString().Contains("ZFŚS"))
                                    kwota += Math.Abs(sumatorworker.Wartosc);
                            }
                            kwota_listy_temp_1 += kwota;
                        }
                        dodaj_do_pozycji_zfss(pozycje_lp, "ZFŚS", kwota_listy_temp_1.ToString(), "851-03-01", "234-03-1");
                        wypisz_podatek_do_us(listaHtWorker, pozycje_lp);


                        break;

                    case "ZFŚS WCZASY": //##########################
                        decimal kwota_listy_temp_2 = 0;
                        for (int i = 0; i < listaHt.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHt[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                if (sumatorworker.Definicja.ToString().Contains("ZFŚS Wczasy"))
                                    kwota += Math.Abs(sumatorworker.Wartosc);
                            }
                            kwota_listy_temp_2 += kwota;
                        }
                        dodaj_do_pozycji_zfss(pozycje_lp, "ZFŚS Wczasy", kwota_listy_temp_2.ToString(), "851-03-01", "234-03-1");
                        wypisz_podatek_do_us(listaHtWorker, pozycje_lp);
                        break;

                    case "ZFŚS LECZENIE SANATORYJNE": //##########################
                        decimal kwota_listy_temp_3 = 0;
                        for (int i = 0; i < listaHt.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHt[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                if (sumatorworker.Definicja.ToString().Contains("ZFŚS Leczenie sanatoryjne"))
                                    kwota += Math.Abs(sumatorworker.Wartosc);
                            }
                            kwota_listy_temp_3 += kwota;
                        }
                        dodaj_do_pozycji_zfss(pozycje_lp, "ZFŚS Leczenie sanatoryjne", kwota_listy_temp_3.ToString(), "851-03-01", "234-03-1");
                        break;

                    case "ODPRAWA EMERYTALNA": //##########################
                        wypisz_element_do_listy_xml(listaHt, lP, pozycje_lp, "Odprawa emerytalna", "231-00-1");
                        wypisz_podatek_do_us(listaHtWorker, pozycje_lp);
                        break;

                    case "PŁACE": //##########################
                                  // start sprawdzania listy czy omijac - PŁACE
                        string sss = "";
                        string symbol = String.Empty;
                        int tempInt = listaHtWorker.Count - 1;
                        for (int i = listaHtWorker.Count - 1; i >= 0; i--)
                        {
                            Sumator.Sumator sumatorworker = null;
                            bool czy_ominac_liste = false;
                            // sprawdzanie czy listy dotyczą tych samych rodzajów zatrudnienia                        
                            if (i == tempInt) //pierwszy
                                symbol = listyPlac[i].Numer.ToString().Remove(3);
                            else
                            {
                                if (symbol != listyPlac[i].Numer.ToString().Remove(3))
                                {
                                    return new MessageBoxInformation("Eksport listy płac")
                                    {
                                        Type = MessageBoxInformationType.Information,
                                        Text = "Listy nie są tego samego typu!",
                                        OKHandler = () => null
                                    };
                                }
                            }

                            foreach (DictionaryEntry element in listaHt[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                string skladnik = sumatorworker.MyList[0].definicja;
                                if (skladnik.Contains("Świadczenie socjalne") || skladnik.Contains("Nagroda jubileusz") || skladnik.Contains("NAGRODA") || skladnik.Contains("Pożyczka ZFM"))
                                    czy_ominac_liste = true;
                            }
                            if (czy_ominac_liste)
                            {
                                listaHt.RemoveAt(i);
                                listaHtWorker.RemoveAt(i);
                                lP.RemoveAt(i);
                            }
                            else
                                sss += "\n" + listyPlac[i].Numer.ToString();
                            //koniec sprawdzania listy  
                        }

                        decimal kwota_listy = 0;
                        // ##### WYNAGRODZENIE #####
                        for (int i = 0; i < listaHtWorker.Count; i++)
                        {
                            KsięgowanieListyWorker ksiegowanie = new KsięgowanieListyWorker();
                            ksiegowanie.Lista = lP[i];
                            decimal kwota = ksiegowanie.Razem.WartośćKUP;
                            kwota_listy += kwota;

                            string konto = "404-00-" + rozpoznaj_liste(lP[i].Numer.ToString()) + "-01";
                            string kontoMA = "231-00-1";
                            if (ksiegowanie.Lista.Definicja.Symbol == "LPU")
                            {
                                konto = "404-00-" + rozpoznaj_liste(lP[i].Numer.ToString()) + "-02";
                                kontoMA = "231-00-2";
                            }
                            dodaj_do_pozycji(true, pozycje_lp, "wynagrodzenie", kwota.ToString(), "401", 0, konto);

                            if (i + 1 == listaHtWorker.Count)
                                dodaj_do_pozycji(false, pozycje_lp, "wynagrodzenie", kwota_listy.ToString(), "401", 0, kontoMA);

                        }
                        kwota_listy = 0;

                        // ##### ZASIŁKI #####
                        for (int i = 0; i < listaHtWorker.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHtWorker[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                kwota += sumatorworker.Zasilkizus;
                            }
                            kwota_listy += kwota;
                        }
                        string temp_konto = "229-00-0-1-03";

                        if (kwota_listy != 0)
                        {
                            dodaj_do_pozycji(true, pozycje_lp, "zasilki zus", kwota_listy.ToString(), "411", 0, temp_konto);
                            dodaj_do_pozycji(false, pozycje_lp, "zasilki zus", kwota_listy.ToString(), "411", 0, "231-00-1");
                        }

                        kwota_listy = 0;

                        // ##### SKŁADKI ZUS PRACODAWCA#####
                        for (int i = 0; i < listaHtWorker.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHtWorker[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                kwota += sumatorworker.Zuspca;
                            }
                            kwota_listy += kwota;

                            string konto = "405-00-" + rozpoznaj_liste(lP[i].Numer.ToString()) + "-01";
                            string kontoMA = "229-00-0-1-03";

                            if (listyPlac[i].Definicja.Symbol == "LPU") // zlecenie
                            {
                                konto = "405-00-" + rozpoznaj_liste(lP[i].Numer.ToString()) + "-01";
                                kontoMA = "229-00-0-1-04";
                            }

                            if (kwota_listy != 0)
                            {
                                dodaj_do_pozycji(true, pozycje_lp, "skladka ZUS pracodawca", kwota.ToString(), "411", 0, konto);
                                if (i + 1 == listaHtWorker.Count)
                                    dodaj_do_pozycji(false, pozycje_lp, "skladka ZUS pracodawca", kwota_listy.ToString(), "411", 0, kontoMA);
                            }
                        }
                        kwota_listy = 0;

                        // ##### FPR #####
                        for (int i = 0; i < listaHtWorker.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHtWorker[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                kwota += sumatorworker.FPR;
                            }
                            kwota_listy += kwota;

                            string konto = "405-00-" + rozpoznaj_liste(lP[i].Numer.ToString()) + "-02";

                            if (kwota_listy != 0)
                            {
                                dodaj_do_pozycji(true, pozycje_lp, "FPR", kwota.ToString(), "412", 0, konto);
                                if (i + 1 == listaHtWorker.Count)
                                    dodaj_do_pozycji(false, pozycje_lp, "FPR", kwota_listy.ToString(), "412", 0, "229-00-0-3");
                            }
                        }
                        kwota_listy = 0;

                        // ##### SKŁADKI ZUS PRACOWNIK #####
                        bool lista_lpu = false;
                        for (int i = 0; i < listaHtWorker.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHtWorker[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                kwota += sumatorworker.Zuspik;
                            }
                            kwota_listy += kwota;
                            if (listyPlac[i].Definicja.Symbol == "LPU") // zlecenie
                                lista_lpu = true;
                        }
                        string konto231 = "231-00-1";
                        string kontoMA2 = "229-00-0-1-01";

                        if (lista_lpu) // zlecenie
                        {
                            konto231 = "231-00-2";
                            kontoMA2 = "229-00-0-1-04";
                        }

                        if (kwota_listy != 0)
                        {
                            dodaj_do_pozycji(true, pozycje_lp, "skladka ZUS pracownik", kwota_listy.ToString(), "401", 0, konto231);
                            dodaj_do_pozycji(false, pozycje_lp, "skladka ZUS pracownik", kwota_listy.ToString(), "401", 0, kontoMA2);
                        }
                        kwota_listy = 0;

                        // ##### UBEZPIECZENIE ZDROWOTNE #####
                        for (int i = 0; i < listaHtWorker.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHtWorker[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                kwota += sumatorworker.PracZdrowotna;
                            }
                            kwota_listy += kwota;
                        }
                        kontoMA2 = "229-00-0-2-01";
                        if (lista_lpu) // zlecenie
                        {
                            konto231 = "231-00-2";
                            kontoMA2 = "229-00-0-2-03";
                        }

                        if (kwota_listy != 0)
                        {
                            dodaj_do_pozycji(true, pozycje_lp, "ubezpieczenie zdrowotne", kwota_listy.ToString(), "401", 0, konto231);
                            dodaj_do_pozycji(false, pozycje_lp, "ubezpieczenie zdrowotne", kwota_listy.ToString(), "401", 0, kontoMA2);
                        }
                        kwota_listy = 0;

                        // ##### PODATEK do US #####
                        for (int i = 0; i < listaHtWorker.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHtWorker[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                kwota += sumatorworker.PracZaliczkaPIT;


                            }
                            kwota_listy += kwota;
                        }

                        kontoMA2 = "225-00-01";
                        if (lista_lpu) // zlecenie
                        {
                            konto231 = "231-00-2";
                            kontoMA2 = "225-00-03";
                        }
                        if (kwota_listy != 0)
                        {
                            dodaj_do_pozycji(true, pozycje_lp, "podatek do US", kwota_listy.ToString(), "401", 0, konto231);
                            dodaj_do_pozycji(false, pozycje_lp, "podatek do US", kwota_listy.ToString(), "401", 0, kontoMA2);
                        }
                        kwota_listy = 0;

                        // ##### POTRĄCENIA - KOMORNI I INNE #####
                        for (int i = 0; i < listaHt.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHt[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                //if(sumatorworker.Definicja.RodzajZrodla == RodzajŹródłaWypłaty.ZajęcieKomornicze)
                                if (sumatorworker.Definicja.ToString().Contains("Zajęcia komornicze") || sumatorworker.Definicja.ToString().Contains("Potrącenie"))
                                    kwota += Math.Abs(sumatorworker.Wartosc);
                            }
                            kwota_listy += kwota;
                            //kwota_listy = Math.Abs(kwota_listy);
                        }
                        if (kwota_listy != 0)
                        {
                            dodaj_do_pozycji(true, pozycje_lp, "potracenia - komornik i inne", kwota_listy.ToString(), "401", 0, konto231);
                            dodaj_do_pozycji(false, pozycje_lp, "potracenia - komornik i inne", kwota_listy.ToString(), "401", 0, "240-00-01-06");
                        }
                        kwota_listy = 0;

                        // ##### SKŁADKA PKZP + SPŁATA POŻYCZKI PKZP + WPISOWE PKZP #####
                        for (int i = 0; i < listaHt.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHt[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                if (sumatorworker.Definicja.ToString().Contains("Składka KZP") || sumatorworker.Definicja.ToString().Contains("Spłata pożyczki KZP") || sumatorworker.Definicja.ToString().Contains("Wpisowe KZP"))
                                    kwota += Math.Abs(sumatorworker.Wartosc);
                            }
                            kwota_listy += kwota;
                        }
                        if (kwota_listy != 0)
                        {
                            dodaj_do_pozycji(true, pozycje_lp, "KZP", kwota_listy.ToString(), "401", 0, konto231);
                            dodaj_do_pozycji(false, pozycje_lp, "KZP", kwota_listy.ToString(), "401", 0, "240-00-01-03");
                        }
                        kwota_listy = 0;

                        // ##### WZZ - Wol. Zw. Zaw. #####
                        for (int i = 0; i < listaHt.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHt[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                if (sumatorworker.Definicja.ToString() == "Wol. Zw. Zaw.")
                                    kwota += sumatorworker.Wartosc;
                            }
                            kwota_listy += kwota;

                        }
                        kwota_listy = Math.Abs(kwota_listy);
                        if (kwota_listy != 0)
                        {
                            dodaj_do_pozycji(true, pozycje_lp, "WZZ", kwota_listy.ToString(), "401", 0, konto231);
                            dodaj_do_pozycji(false, pozycje_lp, "WZZ", kwota_listy.ToString(), "401", 0, "240-00-01-04");
                        }
                        kwota_listy = 0;

                        // ##### potrącenia PZU #####
                        for (int i = 0; i < listaHt.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHt[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                if (sumatorworker.Definicja.ToString() == "Składka PZU")
                                    kwota += Math.Abs(sumatorworker.Wartosc);
                            }
                            kwota_listy += kwota;

                        }
                        kwota_listy = Math.Abs(kwota_listy);
                        if (kwota_listy != 0)
                        {
                            dodaj_do_pozycji(true, pozycje_lp, "potracenia PZU", kwota_listy.ToString(), "401", 0, konto231);
                            dodaj_do_pozycji(false, pozycje_lp, "potracenia PZU", kwota_listy.ToString(), "401", 0, "240-00-01-02");
                        }
                        kwota_listy = 0;

                        // ##### ZFM - rozbite na pojedyńczych pracowników #####
                        for (int i = 0; i < listaHt.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHt[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                if (sumatorworker.Definicja.ToString() == "Spłata pożyczki ZFM")
                                {
                                    bool suma_elementow = false;
                                    for (int k = 0; k < sumatorworker.MyList.Count; k++)
                                    {
                                        if (suma_elementow)
                                        {
                                            suma_elementow = false;
                                            continue;
                                        }
                                        if (k != sumatorworker.MyList.Count - 1) // dla elementów od początku do ostatniego - 1
                                        {
                                            if (sumatorworker.MyList[k].prac == sumatorworker.MyList[k + 1].prac) // jeśli następny wpis jest tym samym pracownikiem
                                            {
                                                Pracownik pracownik = sumatorworker.MyList[k].prac;
                                                string kod_pracownika = pracownik.Features["StaryKod"].ToString();
                                                kod_pracownika = kod_pracownika.Remove(0, 2);
                                                dodaj_do_pozycji(true, pozycje_lp, "ZFM", Math.Abs(sumatorworker.MyList[k].kwota + sumatorworker.MyList[k + 1].kwota).ToString(), "401", 0, konto231);
                                                dodaj_do_pozycji(false, pozycje_lp, "ZFM", Math.Abs(sumatorworker.MyList[k].kwota + sumatorworker.MyList[k + 1].kwota).ToString(), "401", 0, "234-03-2-" + kod_pracownika);
                                                suma_elementow = true;
                                            }
                                            else
                                            {
                                                Pracownik pracownik = sumatorworker.MyList[k].prac;
                                                string kod_pracownika = pracownik.Features["StaryKod"].ToString();
                                                kod_pracownika = kod_pracownika.Remove(0, 2);
                                                dodaj_do_pozycji(true, pozycje_lp, "ZFM", Math.Abs(sumatorworker.MyList[k].kwota).ToString(), "401", 0, konto231);
                                                dodaj_do_pozycji(false, pozycje_lp, "ZFM", Math.Abs(sumatorworker.MyList[k].kwota).ToString(), "401", 0, "234-03-2-" + kod_pracownika);
                                            }

                                        }
                                        else if (k == sumatorworker.MyList.Count - 1) // dla ostatniego elementu tablicy
                                        {
                                            Pracownik pracownik = sumatorworker.MyList[k].prac;
                                            string kod_pracownika = pracownik.Features["StaryKod"].ToString();
                                            kod_pracownika = kod_pracownika.Remove(0, 2);
                                            dodaj_do_pozycji(true, pozycje_lp, "ZFM", Math.Abs(sumatorworker.MyList[k].kwota).ToString(), "401", 0, konto231);
                                            dodaj_do_pozycji(false, pozycje_lp, "ZFM", Math.Abs(sumatorworker.MyList[k].kwota).ToString(), "401", 0, "234-03-2-" + kod_pracownika);
                                        }
                                    }
                                }
                            }
                        }

                        // ##### SITK #####
                        for (int i = 0; i < listaHt.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHt[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                if (sumatorworker.Definicja.ToString() == "SITK")
                                    kwota += Math.Abs(sumatorworker.Wartosc);
                            }
                            kwota_listy += kwota;
                        }
                        if (kwota_listy != 0)
                        {
                            dodaj_do_pozycji(true, pozycje_lp, "skladka SITK", kwota_listy.ToString(), "401", 0, konto231);
                            dodaj_do_pozycji(false, pozycje_lp, "skladka SITK", kwota_listy.ToString(), "401", 0, "240-00-01-01");
                        }
                        kwota_listy = 0;

                        // ##### ALIMENTY #####
                        for (int i = 0; i < listaHt.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHt[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                string nazwaSkladnika = sumatorworker.Definicja.ToString().ToUpper();
                                if (nazwaSkladnika.Contains("ALIMENTY"))
                                    kwota += sumatorworker.Wartosc;
                            }
                            kwota_listy += Math.Abs(kwota);
                        }
                        if (kwota_listy != 0)
                        {
                            dodaj_do_pozycji(true, pozycje_lp, "alimenty", kwota_listy.ToString(), "401", 0, konto231);
                            dodaj_do_pozycji(false, pozycje_lp, "alimenty", kwota_listy.ToString(), "401", 0, "240-00-01-07");
                        }
                        kwota_listy = 0;

                        // ##### EKWIWALENT ZA PRANIE #####
                        for (int i = 0; i < listaHt.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHt[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                if (sumatorworker.Definicja.ToString() == "Ekwiwalent za pranie")
                                    kwota += Math.Abs(sumatorworker.Wartosc);
                            }
                            kwota_listy += kwota;

                            string konto = "405-00-" + rozpoznaj_liste(lP[i].Numer.ToString()) + "-05";
                            if (kwota_listy != 0)
                            {
                                if (kwota != 0)
                                    dodaj_do_pozycji(true, pozycje_lp, "ekwiwalent za pranie", kwota.ToString(), "411", 0, konto);
                                if (i + 1 == listaHtWorker.Count)
                                    dodaj_do_pozycji(false, pozycje_lp, "ekwiwalent za pranie", kwota_listy.ToString(), "411", 0, "234-00-4");
                            }
                        }
                        kwota_listy = 0;

                        // ##### PPK PRACOWNIK #####
                        for (int i = 0; i < listaHtWorker.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHtWorker[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                kwota += sumatorworker.PPKprac;
                            }
                            kwota_listy += kwota;
                            kwota_listy = Math.Abs(kwota_listy);
                        }
                        if (kwota_listy != 0)
                        {
                            dodaj_do_pozycji(true, pozycje_lp, "PPK pracownik", kwota_listy.ToString(), "401", 0, konto231);
                            dodaj_do_pozycji(false, pozycje_lp, "PPK pracownik", kwota_listy.ToString(), "401", 0, "240-00-01-08");
                        }
                        kwota_listy = 0;

                        // ##### PPK PRACODAWCA #####
                        for (int i = 0; i < listaHtWorker.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHtWorker[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                kwota += sumatorworker.PPKplat;
                            }
                            kwota_listy += kwota;
                            kwota_listy = Math.Abs(kwota_listy);

                            string konto = "405-00-" + rozpoznaj_liste(lP[i].Numer.ToString()) + "-12";
                            if (kwota_listy != 0)
                            {
                                dodaj_do_pozycji(true, pozycje_lp, "PPK pracodawca", kwota.ToString(), "411", 0, konto);
                                if (i + 1 == listaHtWorker.Count)
                                    dodaj_do_pozycji(false, pozycje_lp, "PPK pracodawca", kwota_listy.ToString(), "411", 0, "240-00-01-08");
                            }
                        }
                        kwota_listy = 0;

                        // ##### ZWROT PPK PRACOWNIK #####
                        for (int i = 0; i < listaHt.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHt[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                if (sumatorworker.Definicja.ToString() == "Zwrot składki PPK (uczestnik)")
                                    kwota += Math.Abs(sumatorworker.Wartosc);
                            }
                            kwota_listy += kwota;
                        }
                        kwota_listy = Math.Abs(kwota_listy);

                        if (kwota_listy != 0)
                        {
                            dodaj_do_pozycji(true, pozycje_lp, "zwrot składki PPK (uczestnik)", kwota_listy.ToString(), "401", 0, konto231);
                            dodaj_do_pozycji(false, pozycje_lp, "zwrot składki PPK (uczestnik)", kwota_listy.ToString(), "401", 0, "240-00-01-08");
                        }
                        kwota_listy = 0;

                        //// ##### ZWROT PPK PRACODAWCA #####
                        for (int i = 0; i < listaHt.Count; i++)
                        {
                            decimal kwota = 0;
                            Sumator.Sumator sumatorworker = null;
                            foreach (DictionaryEntry element in listaHt[i])
                            {
                                sumatorworker = (Sumator.Sumator)element.Value;
                                if (sumatorworker.Definicja.ToString() == "Zwrot składki PPK (pracodawca)")
                                    kwota += sumatorworker.Wartosc;
                            }
                            kwota_listy += kwota;

                            string konto = "405-00-" + rozpoznaj_liste(lP[i].Numer.ToString()) + "-12";
                            if (kwota_listy != 0)
                            {
                                dodaj_do_pozycji(true, pozycje_lp, "Zwrot składki PPK (pracodawca)", kwota.ToString(), "411", 0, konto);
                                if (i + 1 == listaHtWorker.Count)
                                    dodaj_do_pozycji(false, pozycje_lp, "Zwrot składki PPK (pracodawca)", kwota_listy.ToString(), "411", 0, "240-00-01-08");
                            }
                        }
                        kwota_listy = 0;

                        if (kwota_listy != 0)
                        {
                            dodaj_do_pozycji(true, pozycje_lp, "PPK pracodawca", kwota_listy.ToString(), "401", 0, konto231);
                            dodaj_do_pozycji(false, pozycje_lp, "PPK pracodawca", kwota_listy.ToString(), "401", 0, "240-00-01-08");
                        }
                        kwota_listy = 0;
                        break;

                    default:
                        return new MessageBoxInformation("Eksport listy płac")
                        {
                            Type = MessageBoxInformationType.Information,
                            Text = "------- NIE TRAFIŁO DO ŻADNEJ DEKLARACJI -------",
                            OKHandler = () => null
                        };
                }
                drzewo_start.Add(pozycje_lp);

                plik.WriteLine(drzewo_start);
                sw.WriteLine(Environment.NewLine + string.Format("{0,-17}", "") + string.Format("{0,14}", kwota_wn) + string.Format("{0,14}", kwota_ma) + string.Format("{0,5}", "") + string.Format("{0,-28}", "SUMA"));
                sw.WriteLine(Environment.NewLine);
                sw.WriteLine("Dane pochodzą z programu Enova365.");
                plik.Close();
                sw.Close();


                //}
            }
            return new MessageBoxInformation("Eksport listy płac")
            {
                Type = MessageBoxInformationType.Information,
                Text = "Zakończono eksport list płac do pliku XML",
                OKHandler = () => null
            };
        }

        private string rozpoznaj_liste(string nazwa_listy)
        {
            int i = 99;
            if (nazwa_listy.Contains("RDW Suwałki"))
                i = 5;
            else if (nazwa_listy.Contains("RDW Siemiatycze"))
                i = 3;
            else if (nazwa_listy.Contains("RDW Sokółka"))
                i = 4;
            else if (nazwa_listy.Contains("RDW Białystok"))
                i = 1;
            else if (nazwa_listy.Contains("A_ZARZ"))
                i = 0;
            else if (nazwa_listy.Contains("RDW Łomża"))
                i = 2;
            else
                i = 88;

            return i.ToString();
        }
        private void dodaj_do_pozycji(bool czy_wn, XElement pozycje_lp, string nazwa, string kwota, string paragraf_, int typ_fin_, string konto_)
        {
            numer_pozycji++;

            XElement pozycja = new XElement("POZYCJA");

            XElement numer = new XElement("NUMER", numer_pozycji);
            pozycja.Add(numer);
            XElement opis = new XElement("OPIS", nazwa);
            pozycja.Add(opis);
            XElement kwota_brutto = new XElement("KWOTA_BRUTTO", kwota);
            pozycja.Add(kwota_brutto);
            XElement zadanie = new XElement("ZADANIE", "1");
            pozycja.Add(zadanie);
            XElement dzial = new XElement("DZIAL", "600");
            pozycja.Add(dzial);
            XElement rodzial = new XElement("ROZDZIAL", "75018");
            pozycja.Add(rodzial);
            XElement paragraf = new XElement("PARAGRAF", paragraf_);
            pozycja.Add(paragraf);
            XElement typ_fin = new XElement("TYP_FIN", typ_fin_);
            pozycja.Add(typ_fin);
            XElement zr_fin = new XElement("ZR_FIN", "");
            pozycja.Add(zr_fin);
            XElement dysp = new XElement("DYSP", "");
            pozycja.Add(dysp);
            XElement konto_wn;
            XElement konto_ma;
            if (czy_wn)
            {
                konto_wn = new XElement("KONTO_WN", konto_);
                pozycja.Add(konto_wn);
                sw.WriteLine(string.Format("{0,-17}", konto_) + string.Format("{0,14}", kwota) + string.Format("{0,14}", "") + string.Format("{0,5}", "") + string.Format("{0,-28}", nazwa));
                kwota_wn += Convert.ToDecimal(kwota);
            }
            else
            {
                konto_ma = new XElement("KONTO_MA", konto_);
                pozycja.Add(konto_ma);
                sw.WriteLine(string.Format("{0,-17}", konto_) + string.Format("{0,14}", "") + string.Format("{0,14}", kwota) + string.Format("{0,5}", "") + string.Format("{0,-28}", nazwa));
                kwota_ma += Convert.ToDecimal(kwota);
            }

            pozycje_lp.Add(pozycja);
        }
        private void dodaj_do_pozycji_zfss(XElement pozycje_lp, string nazwa, string kwota, string konto_wn_, string konto_ma_)
        {
            numer_pozycji++;

            XElement pozycja = new XElement("POZYCJA");

            XElement numer = new XElement("NUMER", numer_pozycji);
            pozycja.Add(numer);
            XElement opis = new XElement("OPIS", nazwa);
            pozycja.Add(opis);
            XElement kwota_brutto = new XElement("KWOTA_BRUTTO", kwota);
            pozycja.Add(kwota_brutto);
            XElement zadanie = new XElement("ZADANIE", "");
            pozycja.Add(zadanie);
            XElement dzial = new XElement("DZIAL", "");
            pozycja.Add(dzial);
            XElement rodzial = new XElement("ROZDZIAL", "");
            pozycja.Add(rodzial);
            XElement paragraf = new XElement("PARAGRAF", "");
            pozycja.Add(paragraf);
            XElement typ_fin = new XElement("TYP_FIN", "0");
            pozycja.Add(typ_fin);
            XElement zr_fin = new XElement("ZR_FIN", "");
            pozycja.Add(zr_fin);
            XElement dysp = new XElement("DYSP", "");
            pozycja.Add(dysp);
            XElement konto_wn;
            XElement konto_ma;
            konto_wn = new XElement("KONTO_WN", konto_wn_);
            pozycja.Add(konto_wn);
            konto_ma = new XElement("KONTO_MA", konto_ma_);
            pozycja.Add(konto_ma);

            kwota_ma += Convert.ToDecimal(kwota);
            kwota_wn += Convert.ToDecimal(kwota);

            pozycje_lp.Add(pozycja);
            sw.WriteLine(string.Format("{0,-17}", konto_wn_) + string.Format("{0,14}", kwota) + string.Format("{0,14}", "") + string.Format("{0,5}", "") + string.Format("{0,-28}", nazwa));
            sw.WriteLine(string.Format("{0,-17}", konto_ma_) + string.Format("{0,14}", "") + string.Format("{0,14}", kwota) + string.Format("{0,5}", "") + string.Format("{0,-28}", nazwa));

        }
        private void wypisz_element_do_listy_xml(List<Hashtable> lista_ht, List<ListaPlac> lista_plac, XElement pozycje_lp, string nazwa_skladnika, string numer_konta)
        {
            decimal kwota_listy = 0;
            for (int i = 0; i < lista_ht.Count; i++)
            {
                decimal kwota = 0;
                Sumator.Sumator sumatorworker = null;
                foreach (DictionaryEntry element in lista_ht[i])
                {
                    sumatorworker = (Sumator.Sumator)element.Value;
                    if (sumatorworker.Definicja.ToString() == nazwa_skladnika)
                        kwota += Math.Abs(sumatorworker.Wartosc);
                }
                kwota_listy += kwota;

                if (kwota != 0)
                    dodaj_do_pozycji(true, pozycje_lp, nazwa_skladnika.ToLower(), kwota.ToString(), "401", 0, "404-00-" + rozpoznaj_liste(lista_plac[i].Numer.ToString()) + "-01");
                if (i + 1 == lista_ht.Count && kwota_listy != 0)
                    dodaj_do_pozycji(false, pozycje_lp, nazwa_skladnika.ToLower(), kwota_listy.ToString(), "401", 0, numer_konta);

                //kwota_listy = 0;

            }
            //if (wypisz_podatki)
            //    wypisz_podatki_do_listy_xml(lista_ht_worker, lista_plac, pozycje_lp);
        }
        private void wypisz_podatek_do_us(List<Hashtable> lista_ht_worker, XElement pozycje_lp)
        {
            decimal kwota_listy = 0;
            //##### PODATEK do US #####
            for (int i = 0; i < lista_ht_worker.Count; i++)
            {
                decimal kwota = 0;
                Sumator.Sumator sumatorworker = null;
                foreach (DictionaryEntry element in lista_ht_worker[i])
                {
                    sumatorworker = (Sumator.Sumator)element.Value;
                    kwota += sumatorworker.PracZaliczkaPIT;


                }
                kwota_listy += kwota;
                if (kwota_listy != 0)
                {
                    if (kwota != 0)
                        dodaj_do_pozycji(true, pozycje_lp, "podatek do US", kwota.ToString(), "401", 0, "231-00-1");
                    if (i + 1 == lista_ht_worker.Count)
                        dodaj_do_pozycji(false, pozycje_lp, "podatek do US", kwota_listy.ToString(), "401", 0, "225-00-01");
                }
            }

            kwota_listy = 0;
        }
        private void wypisz_podatki_do_listy_xml(List<Hashtable> lista_ht_worker, List<ListaPlac> lista_plac, XElement pozycje_lp)
        {
            // ##### SKŁADKI ZUS PRACODAWCA#####
            decimal kwota_listy = 0;
            for (int i = 0; i < lista_ht_worker.Count; i++)
            {
                decimal kwota = 0;
                Sumator.Sumator sumatorworker = null;
                foreach (DictionaryEntry element in lista_ht_worker[i])
                {
                    sumatorworker = (Sumator.Sumator)element.Value;
                    kwota += sumatorworker.Zuspca;
                }
                kwota_listy += kwota;

                string konto = "405-00-" + rozpoznaj_liste(lista_plac[i].Numer.ToString()) + "-01";
                if (kwota_listy != 0)
                {
                    dodaj_do_pozycji(true, pozycje_lp, "skladka ZUS pracodawca", kwota.ToString(), "411", 0, konto);

                    if (i + 1 == lista_ht_worker.Count)
                        dodaj_do_pozycji(false, pozycje_lp, "skladka ZUS pracodawca", kwota_listy.ToString(), "411", 0, "229-00-0-1-03");
                }
            }
            kwota_listy = 0;
            // ##### FPR #####
            for (int i = 0; i < lista_ht_worker.Count; i++)
            {
                decimal kwota = 0;
                Sumator.Sumator sumatorworker = null;
                foreach (DictionaryEntry element in lista_ht_worker[i])
                {
                    sumatorworker = (Sumator.Sumator)element.Value;
                    kwota += sumatorworker.FPR;
                }
                kwota_listy += kwota;

                string konto = "405-00-" + rozpoznaj_liste(lista_plac[i].Numer.ToString()) + "-02";
                if (kwota_listy != 0)
                {
                    dodaj_do_pozycji(true, pozycje_lp, "FPR", kwota.ToString(), "412", 0, konto);

                    if (i + 1 == lista_ht_worker.Count)
                        dodaj_do_pozycji(false, pozycje_lp, "FPR", kwota_listy.ToString(), "412", 0, "229-00-0-3");
                }
            }
            kwota_listy = 0;
            // ##### SKŁADKA ZUS - PRACOWNIK #####
            for (int i = 0; i < lista_ht_worker.Count; i++)
            {
                decimal kwota = 0;
                Sumator.Sumator sumatorworker = null;
                foreach (DictionaryEntry element in lista_ht_worker[i])
                {
                    sumatorworker = (Sumator.Sumator)element.Value;
                    kwota += sumatorworker.Zuspik;
                }
                kwota_listy += kwota;
                if (kwota_listy != 0)
                {
                    dodaj_do_pozycji(true, pozycje_lp, "skladka ZUS - pracownik", kwota.ToString(), "401", 0, "231-00-1");
                    if (i + 1 == lista_ht_worker.Count)
                        dodaj_do_pozycji(false, pozycje_lp, "skladka ZUS - pracownik", kwota_listy.ToString(), "401", 0, "229-00-0-1-01");
                }
            }

            kwota_listy = 0;
            // ##### UBEZPIECZENIE ZDROWOTNE #####
            for (int i = 0; i < lista_ht_worker.Count; i++)
            {
                decimal kwota = 0;
                Sumator.Sumator sumatorworker = null;
                foreach (DictionaryEntry element in lista_ht_worker[i])
                {
                    sumatorworker = (Sumator.Sumator)element.Value;
                    kwota += sumatorworker.PracZdrowotna;
                }
                kwota_listy += kwota;
                if (kwota_listy != 0)
                {
                    dodaj_do_pozycji(true, pozycje_lp, "ubezpieczenie zdrowotne", kwota.ToString(), "401", 0, "231-00-1");
                    if (i + 1 == lista_ht_worker.Count)
                        dodaj_do_pozycji(false, pozycje_lp, "ubezpieczenie zdrowotne", kwota_listy.ToString(), "401", 0, "229-00-0-2-01");
                }
            }

            kwota_listy = 0;

            // ##### PODATEK do US #####
            wypisz_podatek_do_us(lista_ht_worker, pozycje_lp);
        }
        private void wypisz_potracenie_komornik(List<Hashtable> lista_ht, XElement pozycje_lp)
        {
            decimal kwota_listy = 0;
            for (int i = 0; i < lista_ht.Count; i++)
            {
                decimal kwota = 0;
                Sumator.Sumator sumatorworker = null;
                foreach (DictionaryEntry element in lista_ht[i])
                {
                    sumatorworker = (Sumator.Sumator)element.Value;
                    if (sumatorworker.Definicja.ToString().Contains("Zajęcia komornicze") || sumatorworker.Definicja.ToString().Contains("Potrącenie"))
                        kwota += Math.Abs(sumatorworker.Wartosc);
                }
                kwota_listy += kwota;
                if (kwota_listy != 0)
                {
                    dodaj_do_pozycji(true, pozycje_lp, "potracenia - komornik i inne", kwota.ToString(), "401", 0, "231-00-1");
                    if (i + 1 == lista_ht.Count)
                        dodaj_do_pozycji(false, pozycje_lp, "potracenia - komornik i inne", kwota_listy.ToString(), "401", 0, "240-00-01-06");
                }
            }

        }
        private void wypisz_ppk_pracownik(List<Hashtable> lista_ht_worker, XElement pozycje_lp)
        {
            decimal kwota_listy = 0;
            for (int i = 0; i < lista_ht_worker.Count; i++)
            {
                decimal kwota = 0;
                Sumator.Sumator sumatorworker = null;
                foreach (DictionaryEntry element in lista_ht_worker[i])
                {
                    sumatorworker = (Sumator.Sumator)element.Value;
                    kwota += sumatorworker.PPKprac;
                }
                kwota_listy += kwota;
                kwota_listy = Math.Abs(kwota_listy);

                if (kwota_listy != 0)
                {
                    dodaj_do_pozycji(true, pozycje_lp, "PPK pracownik", kwota.ToString(), "401", 0, "231-00-1");
                    if (i + 1 == lista_ht_worker.Count)
                        dodaj_do_pozycji(false, pozycje_lp, "PPK pracownik", kwota_listy.ToString(), "401", 0, "240-00-01-08");
                }
            }
        }
        private void wypisz_ppk_pracodawca(List<Hashtable> lista_ht_worker, XElement pozycje_lp, List<ListaPlac> lP)
        {
            // ##### PPK PRACODAWCA #####
            decimal kwota_listy = 0;
            for (int i = 0; i < lista_ht_worker.Count; i++)
            {
                decimal kwota = 0;
                Sumator.Sumator sumatorworker = null;
                foreach (DictionaryEntry element in lista_ht_worker[i])
                {
                    sumatorworker = (Sumator.Sumator)element.Value;
                    kwota += sumatorworker.PPKplat;
                }
                kwota_listy += kwota;
                kwota_listy = Math.Abs(kwota_listy);

                string konto = "405-00-" + rozpoznaj_liste(lP[i].Numer.ToString()) + "-12";
                if (kwota_listy != 0)
                {
                    dodaj_do_pozycji(true, pozycje_lp, "PPK pracodawca", kwota.ToString(), "411", 0, konto);
                    if (i + 1 == lista_ht_worker.Count)
                        dodaj_do_pozycji(false, pozycje_lp, "PPK pracodawca", kwota_listy.ToString(), "411", 0, "240-00-01-08");
                }
            }

        }
        public static bool IsEnabledFunkcja(ListaPlac dokument)
        {
            return dokument.Zatwierdzona;
        }

    }
}
