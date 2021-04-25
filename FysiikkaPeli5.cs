using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;


/// @author Olli Mehtonen
/// @version


public class FysiikkaPeli5 : PhysicsGame
{
    private static readonly Image lumiukko = LoadImage("lumiukkopix");
    private static readonly Image taustaKuva = LoadImage("peliTausta");
    private static readonly Image menuKuva = LoadImage("MenuTausta");
    private static readonly Image lumitaso = LoadImage("taso");
    private static readonly Image lumipalloKuva = LoadImage("lumipallopix");
    private static readonly Image sydanKuva = LoadImage("sydanpix");
    private static readonly Image isoKuva = LoadImage("iso");
    private static readonly Image jaaKuva = LoadImage("jaaPix");
    private static readonly Image tahtiKuva = LoadImage("tahtiPix");
    private static readonly Image[] damage = LoadImages("lumiukkopix", "damage");

    private PlatformCharacter pelaaja1;
    private PlatformCharacter pelaaja2;

    private IntMeter pelaaja1Elamat;
    private IntMeter pelaaja2Elamat;

    private IntMeter pelaaja1Pisteet;
    private IntMeter pelaaja2Pisteet;

    private const double NOPEUS = 200;
    private const double HYPPY_NOPEUS = 200;
    private const double KORKEUS = 60;
    private const double LEVEYS = 60;

    private const int MAX_ELAMAT = 3;

    /// <summary>
    /// Alkuvalikko
    /// </summary>
    public override void Begin()
    {
        SetWindowSize(1024, 768, false);

        MultiSelectWindow mainMenu = new MultiSelectWindow(" ",
        "Aloita peli", "Lopeta");

        mainMenu.AddItemHandler(0, Alku);
        mainMenu.AddItemHandler(1, Exit);
        mainMenu.DefaultCancel = 2;
        mainMenu.Color = Color.Transparent;
        mainMenu.SetButtonColor(Color.Snow);
        mainMenu.SetButtonTextColor(Color.Black);
        Level.Background.Image = menuKuva;
        Add(mainMenu);
    }


    /// <summary>
    /// Aloittaa pelin kun painetaa alkuvalikossa "aloita peli"
    /// </summary>
    public void Alku()
    {
        LuoKentta();
        LisaaElamat();
        LuoNappaimet();
        Timer.CreateAndStart(3.0, LuoMuuttuja);
        LisaaPisteet();
    }


    /// <summary>
	/// Kentän luominen (tasot ja pelaajat) maa.txt tiedostoa käyttäen
	/// </summary>
    public void LuoKentta()
    {
        TileMap ruudut = TileMap.FromLevelAsset("maa");
        ruudut.SetTileMethod('=', LuoTaso);
        ruudut.SetTileMethod('^', LuoPiikki);
        ruudut.Execute(20, 20);

        Gravity = new Vector(0, -1100);

        pelaaja1 = LuoPelaaja(Level.Left + 480.0, -300.0);
        pelaaja2 = LuoPelaaja(Level.Right - 480.0, -300.0);
        Camera.Follow(pelaaja1, pelaaja2);
        Camera.FollowXMargin = 650;
        Camera.FollowYMargin = 300;
        Camera.FollowOffset = new Vector(0, 90);
        Level.Background.Image = taustaKuva;
    }


    /// <summary>
	/// Tasojen luonti, minkä päällä pelaajat pelaavat
	/// </summary>
    public void LuoTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(20, 20);
        taso.Position = paikka;
        taso.Shape = Shape.Rectangle;
        taso.Image = lumitaso;
        AddCollisionHandler(taso, "pallo", CollisionHandler.DestroyBoth);
        Add(taso);
    }

    /// <summary>
    /// Luo satunnaisesti eri muuttujia kentälle
    /// </summary>
    public void LuoMuuttuja()
    {
        PhysicsObject muuttuja1 = LuoObjekti(sydanKuva, Shape.Rectangle, "sydan");
        PhysicsObject muuttuja2 = LuoObjekti(jaaKuva, Shape.Rectangle, "jaa");
        PhysicsObject muuttuja3 = LuoObjekti(tahtiKuva, Shape.Rectangle, "tahti");
        PhysicsObject muuttuja4 = LuoObjekti(isoKuva, Shape.Rectangle, "suur");
        PhysicsObject muuttujat = RandomGen.SelectOne<PhysicsObject>(muuttuja1, muuttuja2, muuttuja3, muuttuja4);
        for (int i = 0; i < 100; i++)
        {
            muuttujat.X = RandomGen.NextInt(-400, 400);
            muuttujat.Y = RandomGen.NextInt(-300, 500);
            Add(muuttujat);
        }
    }


    /// <summary>
    /// Luodaan eri muuttujia
    /// </summary>
    public PhysicsObject LuoObjekti(Image kuva, Shape muoto, string tag)
    {
        PhysicsObject muuttuja = new PhysicsObject(20, 20);
        muuttuja.LinearDamping = 0.90;
        AddCollisionHandler(muuttuja, "piikki", CollisionHandler.DestroyObject);
        muuttuja.TextureFillsShape = true;
        muuttuja.LifetimeLeft = TimeSpan.FromSeconds(10.0);
        muuttuja.Image = kuva;
        muuttuja.Shape = muoto;
        muuttuja.Tag = tag;
        return muuttuja;
    }


    /// <summary>
    /// Näkymättöät piikit, mihin kuolee jos tippuu kartalta
    /// </summary>
    public void LuoPiikki(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject piikki = PhysicsObject.CreateStaticObject(20, 20);
        piikki.Position = paikka;
        piikki.Tag = "piikki";
        piikki.IsVisible = false;
        piikki.CollisionIgnoreGroup = 1;
        Add(piikki);
    }


    /// <summary>
	/// Pelaajien luonti ja niiden tiedot
	/// </summary>
    public PlatformCharacter LuoPelaaja(double x, double y)
    {
        PlatformCharacter pelaaja = new PlatformCharacter(LEVEYS, KORKEUS);
        {
            pelaaja.Mass = 100;
            pelaaja.Shape = Shape.FromImage(lumiukko);
            pelaaja.Image = lumiukko;
            pelaaja.CanMoveOnAir = true;
            pelaaja.X = x;
            pelaaja.Y = y;
            pelaaja.Tag = "pelaaja";
        }

        pelaaja.Weapon = new Cannon(20, 6);
        {
            pelaaja.Weapon.InfiniteAmmo = true;
            pelaaja.Weapon.AmmoIgnoresGravity = false;
            pelaaja.Weapon.Power.Value = 17000;
            pelaaja.Weapon.Power.DefaultValue = 17000;
            pelaaja.Weapon.FireRate = 1.0;
            pelaaja.Weapon.X = 6;
            pelaaja.Weapon.Y = 8;
            pelaaja.Weapon.AttackSound = null;
        }

        AddCollisionHandler(pelaaja, "piikki", PelaajaTippui);
        AddCollisionHandler(pelaaja, "pallo", LumipalloOsuma);
        AddCollisionHandler(pelaaja, "sydan", OsuiSydammeen);
        AddCollisionHandler(pelaaja, "tahti", OsuiTahteen);
        AddCollisionHandler(pelaaja, "jaa", OsuiJaahan);
        AddCollisionHandler(pelaaja, "suur", OsuiSuurennokseen);

        Add(pelaaja);
        return (pelaaja);
    }


    /// <summary>
    /// Mitä käy, kun pelaaja poimii sydämmen (+1hp)
    /// </summary>
    public void OsuiSydammeen(PhysicsObject pelaaja, PhysicsObject sydan)
    {
        if (pelaaja == pelaaja1)
        {
            pelaaja1Elamat.Value += 1;
        }
        else if (pelaaja == pelaaja2)
        {
            pelaaja2Elamat.Value += 1;
        }
        sydan.Destroy();
    }


    /// <summary>
    /// Mitä käy, kun pelaaja poimii tähden (ampuu kovempaa)
    /// </summary>
    public void OsuiTahteen(PhysicsObject pelaaja, PhysicsObject tahti)
    {
        if (pelaaja == pelaaja1)
        {
            pelaaja1.Weapon.FireRate = 3.0;
            pelaaja1.Weapon.Power.Value = 26000;
            pelaaja1.Weapon.Power.DefaultValue = 26000;
        }
        else if (pelaaja == pelaaja2)
        {
            pelaaja2.Weapon.FireRate = 3.0;
            pelaaja2.Weapon.Power.Value = 26000;
            pelaaja2.Weapon.Power.DefaultValue = 26000;
        }
        tahti.Destroy();
    }



    /// <summary>
    /// Mitä käy, kun pelaaja poimii jään (toinen pelaaja liikkuu niinkuin kävelisi jäällä)
    /// </summary>
    public void OsuiJaahan(PhysicsObject pelaaja, PhysicsObject jaa)
    {
        if (pelaaja == pelaaja1)
        {
            pelaaja2.MaintainMomentum = true;
        }
        else if (pelaaja == pelaaja2)
        {
            pelaaja1.MaintainMomentum = true;
        }
        jaa.Destroy();
    }


    /// <summary>
    /// Mitä käy, kun pelaaja poimii suurennuksen (toisesta pelaajasta tulee helpompi maali) 
    /// </summary>
    public void OsuiSuurennokseen(PhysicsObject pelaaja, PhysicsObject suur)
    {
        if (pelaaja == pelaaja1)
        {
            pelaaja2.Size = new Vector(KORKEUS * 2, LEVEYS * 2);
        }
        else if (pelaaja == pelaaja2)
        {
            pelaaja1.Size = new Vector(KORKEUS * 2, LEVEYS * 2);
        }
        suur.Destroy();
    }


    /// <summary>
    /// Mitä käy, kun pelaaja tippuu kartalta
    /// </summary>
    public void PelaajaTippui(PhysicsObject tipahti, PhysicsObject piikki)
    {
        if (tipahti == pelaaja1)
        {
            PelaajaKuoli(pelaaja1);
        }
        else if (tipahti == pelaaja2)
        {
            PelaajaKuoli(pelaaja2);
        }
    }


    /// <summary>
    /// Mitä tapahtuu kun pelaajat osuvat lumipalloilla toisiinsa
    /// TODO: Osuma työntää pelaaja takaisin
    /// </summary>
    public void LumipalloOsuma(PhysicsObject osuttu, PhysicsObject pallo)
    {
        osuttu.Animation = new Animation(damage);
        osuttu.Animation.Start(4);
        osuttu.Animation.FPS = 20;

        if (osuttu == pelaaja1)
        {
            pelaaja1Elamat.Value -= 1;
            if (pelaaja1Elamat < 1)
            {
                PelaajaKuoli(pelaaja1);
            }
        }
        else if (osuttu == pelaaja2)
        {
            pelaaja2Elamat.Value -= 1;
            if (pelaaja2Elamat < 1)
            {
                PelaajaKuoli(pelaaja2);
            }
        }
        pallo.Destroy();
    }


    /// <summary>
    /// Luodaan pelaajien health pointsit
    /// </summary>
    public void LisaaElamat()
    {
        pelaaja1Elamat = LuoElamat(Screen.Left + 80.0, Screen.Top - 20.0);
        pelaaja2Elamat = LuoElamat(Screen.Right - 80.0, Screen.Top - 20.0);
    }


    /// <summary>
    /// Pelaajien HP aliohjelma
    /// TODO: HP näkyy sydämminä, ei numeroina
    /// </summary>
    public IntMeter LuoElamat(double x, double y)
    {
        IntMeter elamat = new IntMeter(3);
        elamat.MaxValue = 5;

        Label hp = new Label();
        hp.BindTo(elamat);
        hp.X = x;
        hp.Y = y;
        hp.TextColor = Color.Red;
        hp.BorderColor = Color.Transparent;
        hp.Color = Color.Transparent;
        Add(hp);

        return elamat;
    }


    /// <summary>
	/// Näppäimet
	/// </summary>
    public void LuoNappaimet()
    {
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Show help");


        ControllerOne.Listen(Button.DPadLeft, ButtonState.Down, PelaajaLiiku, "Pelaaja 1 liikkuu oikealle", pelaaja1, -NOPEUS);
        ControllerOne.Listen(Button.DPadRight, ButtonState.Down, PelaajaLiiku, "Pelaaja 1 liikkuu vasemmalle", pelaaja1, NOPEUS);
        ControllerOne.Listen(Button.A, ButtonState.Down, PelaajaHyppaa, "Pelaaja 1 hyppää", pelaaja1, HYPPY_NOPEUS);
        ControllerOne.Listen(Button.X, ButtonState.Pressed, PelaajaAmpuu, "Pelaaja 1 ampuu", pelaaja1);
        ControllerOne.Listen(Button.B, ButtonState.Pressed, PelaajaKyykkaa, "Pelaaja 1 menee kyykkyyn", pelaaja1);
        ControllerOne.Listen(Button.B, ButtonState.Released, KyykkyYlos, "Pelaaja 1 nousee kyykystä", pelaaja1);
        ControllerOne.Listen(Button.DPadUp, ButtonState.Down, PelaajaTahtaa, "Pelaaja 1 tähtää ylös", pelaaja1.Weapon, Angle.FromDegrees(2));
        ControllerOne.Listen(Button.DPadDown, ButtonState.Down, PelaajaTahtaa, "Pelaaja 1 tähtää alas", pelaaja1.Weapon, Angle.FromDegrees(-2));

        //ControllerOne.ListenAnalog(AnalogControl.RightStick, 0.1, TahtaaO, "Tähtää aseella");

        ControllerTwo.Listen(Button.DPadLeft, ButtonState.Down, PelaajaLiiku, "Pelaaja 2 liikkuu oikealle", pelaaja2, -NOPEUS);
        ControllerTwo.Listen(Button.DPadRight, ButtonState.Down, PelaajaLiiku, "Pelaaja 2 liikkuu vasemmalle", pelaaja2, NOPEUS);
        ControllerTwo.Listen(Button.A, ButtonState.Down, PelaajaHyppaa, "Pelaaja 2 hyppää", pelaaja2, HYPPY_NOPEUS);
        ControllerTwo.Listen(Button.X, ButtonState.Pressed, PelaajaAmpuu, "Pelaaja 2 ampuu", pelaaja2);
        ControllerTwo.Listen(Button.DPadDown, ButtonState.Pressed, PelaajaKyykkaa, "Pelaaja 2 menee kyykkyyn", pelaaja2);
        ControllerTwo.Listen(Button.DPadDown, ButtonState.Released, KyykkyYlos, "Pelaaja 2 nousee kyykystä", pelaaja2);
        ControllerTwo.Listen(Button.DPadUp, ButtonState.Down, PelaajaTahtaa, "Pelaaja 2 tähtää ylös", pelaaja2.Weapon, Angle.FromDegrees(2));
        ControllerTwo.Listen(Button.DPadDown, ButtonState.Down, PelaajaTahtaa, "Pelaaja 2 tähtää alas", pelaaja2.Weapon, Angle.FromDegrees(-2));



        Keyboard.Listen(Key.Right, ButtonState.Down, PelaajaLiiku, "Pelaaja 2 liikkuu oikealle", pelaaja2, NOPEUS);
        Keyboard.Listen(Key.Left, ButtonState.Down, PelaajaLiiku, "Pelaaja 2 liikkuu vasemmalle", pelaaja2, -NOPEUS);
        Keyboard.Listen(Key.Up, ButtonState.Down, PelaajaHyppaa, "Pelaaja 2 hyppää", pelaaja2, HYPPY_NOPEUS);
        Keyboard.Listen(Key.Down, ButtonState.Pressed, PelaajaKyykkaa, "Pelaaja 2 menee kyykyyn", pelaaja2);
        Keyboard.Listen(Key.Down, ButtonState.Released, KyykkyYlos, "Pelaaja 2 menee kyykyyn", pelaaja2);
        Keyboard.Listen(Key.NumPad1, ButtonState.Pressed, PelaajaAmpuu, "Pelaaja 2 ampuu", pelaaja2);
        Keyboard.Listen(Key.NumPad5, ButtonState.Down, PelaajaTahtaa, "Pelaaja 2 tähtää ylös", pelaaja2.Weapon, Angle.FromDegrees(2));
        Keyboard.Listen(Key.NumPad2, ButtonState.Down, PelaajaTahtaa, "Pelaaja 2  tähtää alas", pelaaja2.Weapon, Angle.FromDegrees(-2));

        Keyboard.Listen(Key.D, ButtonState.Down, PelaajaLiiku, "Pelaaja 1 liikkuu oikealle", pelaaja1, NOPEUS);
        Keyboard.Listen(Key.A, ButtonState.Down, PelaajaLiiku, "Pelaaja 1 liikkuu vasemmalle", pelaaja1, -NOPEUS);
        Keyboard.Listen(Key.W, ButtonState.Down, PelaajaHyppaa, "Pelaaja 1 hyppää", pelaaja1, HYPPY_NOPEUS);
        Keyboard.Listen(Key.S, ButtonState.Pressed, PelaajaKyykkaa, "Pelaaja 1 menee kyykkyyn", pelaaja1);
        Keyboard.Listen(Key.S, ButtonState.Released, KyykkyYlos, "Pelaaja 2 menee kyykyyn", pelaaja1);
        Keyboard.Listen(Key.G, ButtonState.Pressed, PelaajaAmpuu, "Pelaaja 1 ampuu", pelaaja1);
        Keyboard.Listen(Key.R, ButtonState.Down, PelaajaTahtaa, "Pelaaja 1 tähtää ylös", pelaaja1.Weapon, Angle.FromDegrees(2));
        Keyboard.Listen(Key.F, ButtonState.Down, PelaajaTahtaa, "Pelaaja 1 tähtää alas", pelaaja1.Weapon, Angle.FromDegrees(-2));
    }


    /// <summary>
	/// Pelaajan liikkuminen
	/// </summary>
    public void PelaajaLiiku(PlatformCharacter pelaaja, double x)
    {
        pelaaja.Walk(x);
    }


    /// <summary>
	/// Pelaajan hyppiminen
	/// </summary>
    public void PelaajaHyppaa(PlatformCharacter pelaaja, double y)
    {
        pelaaja.Jump(y);
    }


    /// <summary>
    /// Pelaajan kyykkääminen
    /// </summary>
    public void PelaajaKyykkaa(PlatformCharacter pelaaja)
    {
        pelaaja.Height = (pelaaja.Height / 2);
    }


    /// <summary>
    /// Pelaaja nousee ylös kyykystä
    /// </summary>
    public void KyykkyYlos(PlatformCharacter pelaaja)
    {
        pelaaja.Height = (pelaaja.Height * 2);
    }


    /// <summary>
	/// Pelaajan ampuminen
	/// </summary>
    public void PelaajaAmpuu(PlatformCharacter pelaaja)
    {
        PhysicsObject lumipallo = pelaaja.Weapon.Shoot();
        if (lumipallo != null)
        {
            lumipallo.Image = lumipalloKuva;
            lumipallo.Size *= 1.2;
            lumipallo.Tag = "pallo";
            lumipallo.CollisionIgnoreGroup = 1;
        }
    }


    /// <summary>
    /// Pelaajan tähtääminen
    /// TODO: Korjaa tähtääminen kun katsotaan vasemmalle
    /// </summary>
    public void PelaajaTahtaa(Weapon pelaajanAse, Angle kulma)
    {
        pelaajanAse.Angle += kulma;
    }


    /*
     * /// TODO: Miksi ei toimi
     * 
    void TahtaaO(AnalogState tatinTila)
    {
        pelaaja1.Weapon.Angle = tatinTila.StateVector.Angle;
    }
    */


    /// <summary>
    /// Kun jompikumpi pelaajista kuolee
    /// </summary>
    public void PelaajaKuoli(PlatformCharacter pelaaja)
    {
        pelaaja.Destroy();
        if (pelaaja == pelaaja1)
        {
            pelaaja2Pisteet.Value += 1;
        }
        if (pelaaja == pelaaja2)
        {
            pelaaja1Pisteet.Value += 1;
        }
        Timer.SingleShot(1, UusiEra);
        if (pelaaja1Pisteet == 3)
        {
            Voitto(pelaaja2, pelaaja1);
        }
        else if (pelaaja2Pisteet == 3)
        {
            Voitto(pelaaja1, pelaaja2);
        }
    }

    /// <summary>
    /// Aloitetaan uusi erä
    /// TODO: Toimiva versio
    /// </summary>
    public void UusiEra()
    {
        pelaaja1.Destroy();
        pelaaja2.Destroy();
        LuoKentta();
        pelaaja1Elamat.SetValue(MAX_ELAMAT);
        pelaaja2Elamat.SetValue(MAX_ELAMAT); 
        Keyboard.Clear();
        LuoNappaimet();
    }


    /// <summary>
    /// Luodaan pistetaulukko
    /// </summary>
    public void LisaaPisteet()
    {
        pelaaja1Pisteet = LuoPisteet(Screen.Left + 80.0, Screen.Top - 40.0);
        pelaaja2Pisteet = LuoPisteet(Screen.Right - 80.0, Screen.Top - 40.0);
    }


    /// <summary>
    ///  Näyttäät pelaajien pisteet (paras kolmesta)
    /// </summary>
    public IntMeter LuoPisteet(double x, double y)
    {
        IntMeter pist = new IntMeter(0);
        pist.MaxValue = 3;

        Label pisteet = new Label();
        pisteet.BindTo(pist);
        pisteet.X = x;
        pisteet.Y = y;
        pisteet.TextColor = Color.Gold;
        pisteet.BorderColor = Color.Transparent;
        pisteet.Color = Color.Transparent;
        Add(pisteet);

        return pist;
    }

    /// <summary>
    /// Mitä tapahtuu, kun jompikumpi pelaaja voittaa plein, eli saa 3 pistettä
    /// </summary>
    public void Voitto(PlatformCharacter haviaja, PlatformCharacter voittaja)
    {
        ClearTimers();
        Camera.StopFollowing();
        Camera.Follow(voittaja);
        Camera.Zoom(3);
        MessageDisplay.Add("Voitit pelin! :^)");
        MessageDisplay.AbsolutePosition = new Vector(400, 200);
        MessageDisplay.Font = Font.DefaultHugeBold;
        MessageDisplay.BackgroundColor = Color.Snow;
        Timer.SingleShot(4.0, Restart);
    }


    /// <summary>
    /// Aloittaa matsin uudestaan, kun jompikumpi pelaaja kuolee
    /// </summary>
    public void Restart()
    {
        ClearAll();
        Begin();
    }
}