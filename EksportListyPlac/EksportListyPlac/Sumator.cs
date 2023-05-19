using Soneta.Business;
using Soneta.Kadry;
using Soneta.Place;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sumator
{
    public struct Location
    {
        //public string idPrac;
        public Pracownik prac;
        public string Skladnik;
        public string definicja;
        public decimal kwota;
    }
    public class Sumator
    {
        readonly DefinicjaElementu definicja;
        readonly Wydzial wydzial;
        readonly Pracownik pracownik;
        readonly string stanowisko;
        readonly string numerlisty;
        decimal wartosc;
        decimal fis;
        decimal zdrowotne;
        decimal zdrowotne2;
        decimal doOdliczenia;
        decimal emerprac;
        decimal emerfirma;
        decimal rentprac;
        decimal rentfirma;
        decimal chor;
        decimal wyp;
        decimal fp;
        decimal fgsp;
        decimal fep;
        decimal dowyplaty;
        decimal brutto;
        decimal potracenia;
        decimal zuspik;
        decimal pit4;
        decimal zasilkizus;
        decimal zuspca;
        decimal swiadczenia;
        ListaPlac listaPlac;
        decimal socjalDoOpodatkowania;
        decimal ppkprac;
        decimal ppkplat;
        decimal pzu;
        decimal firmaEmerytalna;
        decimal firmaRentowa;
        decimal firmaWypadkowa;
        decimal pracRentowa;
        decimal pracEmerytalna;
        decimal pracChorobowe;
        decimal pracZdrowotne;
        decimal buzdzetEmerytalna;
        decimal buzdzetRentowa;
        decimal firmaFundPracy;
        decimal firmaFGSP;
        decimal pracZaliczkaPIT;
        decimal firmaPPK;
        decimal pracPPK;
        decimal doWyplaty;
        decimal fpr;
        decimal nadgodziny = 0.00m;



        public List<Location> MyList;

        public Sumator(Wydzial wydzial, DefinicjaElementu definicja)
        {
            this.definicja = definicja;
            this.wydzial = wydzial;
            MyList = new List<Location>();
        }

        public Sumator(Pracownik pracownik, DefinicjaElementu definicja)
        {
            this.definicja = definicja;
            this.pracownik = pracownik;
            MyList = new List<Location>();
        }
        public Sumator(Pracownik pracownik, ListaPlac listaplac)
        {
            this.listaPlac = listaplac;
            this.pracownik = pracownik;
            MyList = new List<Location>();
        }

        public Sumator(string stanowisko, DefinicjaElementu definicja)
        {
            this.definicja = definicja;
            this.stanowisko = stanowisko;
        }

        public Sumator(ListaPlac listaplac)
        {
            //this.numerlisty = listaplac.Numer.ToString();
            this.listaPlac = listaplac;
            this.wydzial = listaplac.Wydzial;
        }

        public void Add(WypElement element)
        {
            Location MyLocation = new Location();

            wartosc += element.Wartosc;
            //fis += element.Podatki.ZalFIS;
            //zdrowotne += element.Podatki.Zdrowotna.Prac;
            //doOdliczenia += element.Podatki.ZdrowotneDoOdliczenia;
            //emerprac += element.Podatki.Emerytalna.Prac;
            //emerfirma += element.Podatki.Emerytalna.Firma;
            //rentprac += element.Podatki.Rentowa.Prac;
            //rentfirma += element.Podatki.Rentowa.Firma;
            //chor += element.Podatki.Chorobowa.Składka;
            //wyp += element.Podatki.Wypadkowa.Składka;
            //fp += element.Podatki.FP.Skladka;
            //fgsp += element.Podatki.FGSP.Skladka;
            //fep += element.Podatki.FEP.Skladka;
            //dowyplaty += element.DoWypłaty;

            //if (element.Definicja.RodzajZrodla == RodzajŹródłaWypłaty.Świadczenie)
            //{
            //    swiadczenia += element.Wartosc;
            //}

            //string pracId = element.PracHistoria.NumerAkt.ToString();
            // int lgt = pracId.IndexOf("/");
            //if (lgt > -1)
            //   pracId = pracId.Substring(pracId.IndexOf("/"));

            //if (element.Definicja.RodzajZrodla == RodzajŹródłaWypłaty.Pożyczka
            //   || element.Definicja.RodzajZrodla == RodzajŹródłaWypłaty.PożyczkaSpłata)
            //{
            //MyLocation.idPrac = element.Pracownik.Kod;
            MyLocation.prac = element.Pracownik;// element.PracHistoria.ID.ToString();
            MyLocation.Skladnik = element.Nazwa;
            MyLocation.kwota = element.Wartosc;
            MyLocation.definicja = element.Definicja.Nazwa.ToString();
            string temp = element.Definicja.NazwaPomocnicza.ToString();

            MyList.Add(MyLocation);
        }


        public void addFromWorker(Wyplata wyplata)
        {

            if (wyplata.Typ == TypWyplaty.Etat)
            {
                //KsięgowanieListyWorker workKs = new KsięgowanieListyWorker();
                //workKs.Lista = listaPlac;

                WyplataSkładkiWorker zusy = new WyplataSkładkiWorker(); ;
                zusy.Wypłata = (WyplataEtat)wyplata;

                WyplataEtatWorker worker = new WyplataEtatWorker();
                worker.Wypłata = (WyplataEtat)wyplata;

                Wyplata.PITInfoWorker podatki = new Wyplata.PITInfoWorker();
                podatki.Wypłata = wyplata;



                doWyplaty = wyplata.DoWypłaty.Value;

                brutto += worker.Brutto;
                potracenia += worker.Potrącenia + worker.NPotrącenia;
                zuspik += worker.SkładkiZUS;
                pit4 += worker.Podatek;
                zasilkizus += worker.Zasiłki; //zasiłki
                zuspca += zusy.Razem.FirmaZUS;
                zdrowotne2 += zusy.Razem.Zdrowotna.Prac;
                dowyplaty += wyplata.DoWypłaty.Value;
                wyp = zusy.Razem.Wypadkowa.Firma;
                ppkprac = zusy.Razem.PPK.Pracownika;
                ppkplat = zusy.Razem.PPK.Pracodawcy;

                firmaEmerytalna = zusy.Razem.Emerytalna.Firma;
                firmaRentowa = zusy.Razem.Rentowa.Firma;
                firmaWypadkowa = zusy.Razem.Wypadkowa.Firma;
                pracEmerytalna = zusy.Razem.Emerytalna.Prac;
                pracRentowa = zusy.Razem.Rentowa.Prac;
                pracChorobowe = zusy.Razem.Chorobowa.Prac;
                pracZdrowotne = zusy.Razem.Zdrowotna.Prac;
                buzdzetEmerytalna = zusy.Razem.EmerytalnaBudzet.Budzet;
                buzdzetRentowa = zusy.Razem.RentowaBudzet.Budzet;
                firmaFundPracy = zusy.Razem.FP.Firma;
                firmaFGSP = zusy.Razem.FGSP.Firma;
                firmaPPK = zusy.Razem.PPK.DodPracodawcy;
                pracPPK = zusy.Razem.PPK.DodPracownika;
                pracZaliczkaPIT = podatki.ZalFIS; //podatek do US
                fpr = zusy.Razem.FP.Firma;

            }

            if (wyplata.Typ == TypWyplaty.Umowa)
            {
                WyplataSkładkiWorker zusy = new WyplataSkładkiWorker(); ;
                zusy.Wypłata = (WyplataUmowa)wyplata;

                WyplataUmowaWorker worker = new WyplataUmowaWorker();
                worker.Wypłata = (WyplataUmowa)wyplata;

                Wyplata.PITInfoWorker podatki = new Wyplata.PITInfoWorker();
                podatki.Wypłata = wyplata;

                doWyplaty = wyplata.DoWypłaty.Value;

                brutto += worker.Brutto;// - worker.NPotrącenia;
                potracenia += worker.Potrącenia + worker.NPotrącenia;
                zuspik += worker.SkładkiZUS;
                pit4 += worker.Podatek;
                zasilkizus += worker.Zasiłki;
                zuspca += zusy.Razem.FirmaZUS;//  .NarzutyFirma;
                zdrowotne2 += zusy.Razem.Zdrowotna.Prac;
                dowyplaty += wyplata.DoWypłaty.Value;
                wyp = zusy.Razem.Wypadkowa.Firma;

                firmaEmerytalna = zusy.Razem.Emerytalna.Firma;
                firmaRentowa = zusy.Razem.Rentowa.Firma;
                firmaWypadkowa = zusy.Razem.Wypadkowa.Firma;
                pracEmerytalna = zusy.Razem.Emerytalna.Prac;
                pracRentowa = zusy.Razem.Rentowa.Prac;
                pracChorobowe = zusy.Razem.Chorobowa.Prac;
                pracZdrowotne = zusy.Razem.Zdrowotna.Prac;
                buzdzetEmerytalna = zusy.Razem.EmerytalnaBudzet.Budzet;
                buzdzetRentowa = zusy.Razem.RentowaBudzet.Budzet;
                firmaFundPracy = zusy.Razem.FP.Firma;
                firmaFGSP = zusy.Razem.FGSP.Firma;
                firmaPPK = zusy.Razem.PPK.DodPracodawcy;
                pracPPK = zusy.Razem.PPK.DodPracownika;
                pracZaliczkaPIT = podatki.ZalFIS;

                //colBrutto.Text = podatki.DoOpodatkowania.Value.ToString("N");
                //colZusPrac.Text = podatki.KosztyFIS.ToString("N");
                //tableCell15.Text = podatki.SkładkiZUS.ToString("N");

                //colPotraceniaN.Text = worker.NPotrącenia.ToString("N");
                //colZasilkiN.Text = worker.NZasiłki.ToString("N");

                //dodatkiB += worker.Dodatki + worker.NieZUS;
                //brutto += worker.Brutto;
                //netto += wypłata.WartoscCy;
                //zasadnicze += worker.Brutto;
                //dodatkiN += worker.NDodatki;
                //potraceniaB += worker.Potrącenia;
                //zasilkiB += worker.Zasiłki;
                //bruttoSum += podatki.DoOpodatkowania;
                //skladkiZus += podatki.KosztyFIS;
                //kosztySum += podatki.SkładkiZUS;
                //podatekSum += podatki.ZalFIS;
                //potraceniaNSum += worker.NPotrącenia;
                //zasilkiNSum += worker.NZasiłki;
            }

            // DEFINIOWANE ELEMENTY WYNAGRODZENIA

            var view = wyplata.Elementy.CreateView();
            view.Condition &= new FieldCondition.Like(nameof(WypElement.Nazwa), "*PZU*");

            foreach (WypElement element in view)
            {
                pzu += element.Wartosc;
            }

            View v;
            view = wyplata.Elementy.CreateView();
            //view.Condition &= new FieldCondition.Like(nameof(WypElement.Nazwa), "*PZU*");

            foreach (WypElement element in view)
            {
                v = element.Skladniki.CreateView();
                v.Condition &= new FieldCondition.Equal("Rodzaj", RodzajSkładnikaWypłaty.OdchyłkaPlus);

                foreach (WypSkladnik ws in v)
                {
                    nadgodziny += ws.Wartosc;
                }
            }
        }

        public string Stanowisko { get { return stanowisko; } }
        public Wydzial Wydzial { get { return wydzial; } }
        public Pracownik Pracownik { get { return pracownik; } }
        public DefinicjaElementu Definicja { get { return definicja; } }
        public decimal Wartosc { get { return wartosc; } }
        public decimal FIS { get { return fis; } }
        public decimal Zdrowotna { get { return zdrowotne; } }
        public decimal Zdrowotna2 { get { return zdrowotne2; } }
        public decimal DoOdliczenia { get { return doOdliczenia; } }
        public decimal EmerFirma { get { return emerfirma; } }
        public decimal EmerPrac { get { return emerprac; } }
        public decimal RentFirma { get { return rentfirma; } }
        public decimal RentPrac { get { return rentprac; } }
        public decimal Chor { get { return chor; } }
        public decimal Wyp { get { return wyp; } }
        public decimal FP { get { return fp; } }
        public decimal FGSP { get { return fgsp; } }
        public decimal FEP { get { return fep; } }
        public decimal DoWyplaty { get { return dowyplaty; } }
        public decimal Brutto { get { return brutto; } }
        public decimal Potracenia { get { return potracenia; } }
        public decimal Zuspik { get { return zuspik; } }
        public decimal Pit4 { get { return pit4; } }
        public decimal Zasilkizus { get { return zasilkizus; } }
        public decimal Zuspca { get { return zuspca; } }
        public decimal Swiadczenia { get { return swiadczenia; } }
        public decimal SocjalDoOpodatkowania { get { return socjalDoOpodatkowania; } }
        public decimal PPKprac { get { return ppkprac; } }
        public decimal PPKplat { get { return ppkplat; } }
        public decimal PZU { get { return pzu; } }
        public decimal FirmaEmerytalna { get { return firmaEmerytalna; } }
        public decimal FirmaRentowa { get { return firmaRentowa; } }
        public decimal FirmaWypadkowa { get { return firmaWypadkowa; } }
        public decimal PracEmerytalna { get { return pracEmerytalna; } }
        public decimal PracRentowa { get { return pracRentowa; } }
        public decimal PracChorobowa { get { return pracChorobowe; } }
        public decimal PracZdrowotna { get { return pracZdrowotne; } }
        public decimal BudzetEmerytalna { get { return buzdzetEmerytalna; } }
        public decimal BudzetRentowa { get { return buzdzetRentowa; } }
        public decimal FirmaFundPracy { get { return firmaFundPracy; } }
        public decimal FirmaFGSP { get { return firmaFGSP; } }
        public decimal FirmaPPK { get { return firmaPPK; } }
        public decimal PracPPK { get { return pracPPK; } }
        public decimal PracZaliczkaPIT { get { return pracZaliczkaPIT; } }
        public decimal SumDoWyplaty { get { return doWyplaty; } }
        public decimal Nadgodziny { get { return nadgodziny; } }
        public decimal FPR { get { return fpr; } }

    }
}
