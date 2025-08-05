using Plugin.LocalNotification;

namespace ZakladysTelefonemMAUI;

public partial class MainPage : ContentPage
{
    // Toto je privátní proměnná na úrovni celé třídy. Slouží k uchování informací
    // o kontaktu, který uživatel vybral. Je definována zde, aby byla dostupná
    // ve více metodách (v OnVybratKontaktClicked se naplní, v OnVytocitCisloClicked se použije).
    private Contact vybranyKontakt;

    // Uchová cestu k souboru s obrázkem, který byl vyfocen nebo vybrán z galerie.
    // Potřebujeme ji mít na úrovni třídy, aby byla přístupná jak metodám pro načtení obrázku,
    // tak i nové metodě pro jeho sdílení.
    private string cestaKNahranemuObrazku;

    // Toto je konstruktor třídy MainPage. Je to speciální metoda, která se zavolá
    // automaticky při každém vytvoření nové instance této stránky.
    public MainPage()
    {
        // Tento řádek je nezbytný. Načte obsah ze souboru MainPage.xaml a "postaví"
        // z něj uživatelské rozhraní (tlačítka, popisky atd.).
        InitializeComponent();

        // Ihned po sestavení stránky zavoláme naši metodu pro načtení informací o zařízení.
        NactiInformace();
    }

    /// <summary>
    /// Tato metoda se zavolá pokaždé, když se stránka zobrazí na obrazovce.
    /// Je to ideální místo pro kontrolu oprávnění a aktualizaci UI.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await NastavTlacitkoNotifikace();

        // Zkontrolujeme, zda zařízení podporuje kompas.
        if (Compass.Default.IsSupported)
        {
            // Přihlásíme se k odběru události 'ReadingChanged'. Naše metoda Compass_ReadingChanged
            // se teď zavolá pokaždé, když kompas zaznamená změnu.
            Compass.Default.ReadingChanged += Compass_ReadingChanged;

            // Spustíme monitorování kompasu. Jako parametr můžeme určit rychlost snímání.
            Compass.Default.Start(SensorSpeed.UI);
        }
    }

    protected override void OnDisappearing()
    {
        // Tato metoda se zavolá vždy, když uživatel opustí stránku.
        base.OnDisappearing();

        // Pokud kompas běží, je zásadní ho zastavit, abychom šetřili baterii.
        if (Compass.Default.IsSupported)
        {
            // Odhlásíme se z odběru události.
            Compass.Default.ReadingChanged -= Compass_ReadingChanged;

            // Zastavíme monitorování.
            Compass.Default.Stop();
        }
    }

    /* ==================================================================================
     * LEKCE 1: Informace o zařízení a konektivitě
     *
     * Požadovaná oprávnění v AndroidManifest.xml (standardně již zaškrtnutá):
     * - <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
     * - <uses-permission android:name="android.permission.INTERNET" />
     * ================================================================================== */

    /// <summary>
    /// Načte informace o zařízení a stavu sítě a zobrazí je v příslušných popiscích (Label).
    /// </summary>
    private void NactiInformace()
    {
        // Využíváme statickou třídu DeviceInfo, která poskytuje přístup k informacím
        // o hardwaru a platformě, na které aplikace běží.
        LabelModel.Text = $"Model telefonu: {DeviceInfo.Current.Model}";
        LabelVyrobce.Text = $"Výrobce: {DeviceInfo.Current.Manufacturer}";
        LabelVerzeOS.Text = $"Verze systému: {DeviceInfo.Current.VersionString}";
        LabelTypZarizeni.Text = $"Typ zařízení: {DeviceInfo.Current.DeviceType}";

        // Třída Connectivity nám umožňuje zjistit stav síťového připojení.
        // NetworkAccess je výčtový typ (enum), který může mít hodnoty jako Internet, Local, None atd.
        NetworkAccess accessType = Connectivity.Current.NetworkAccess;
        LabelPripojeni.Text = $"Přístup k internetu: {accessType}";

        // Zjistíme, jestli je zařízení připojeno k internetu.
        if (accessType == NetworkAccess.Internet)
        {
            // Pokud ano, zjistíme, jakým způsobem (Wi-Fi, mobilní data...).
            // Může být aktivních více profilů najednou.
            var profily = Connectivity.Current.ConnectionProfiles;
            // Spojíme názvy všech profilů do jednoho textového řetězce odděleného čárkou.
            LabelTypPripojeni.Text = $"Typ připojení: {string.Join(", ", profily)}";
        }
        else
        {
            LabelTypPripojeni.Text = "Typ připojení: Není k dispozici";
        }
    }

    /* ==================================================================================
     * LEKCE 2: Geolokace (GPS)
     *
     * Požadovaná oprávnění v AndroidManifest.xml:
     * - <uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
     * - <uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
     * ================================================================================== */

    /// <summary>
    /// Metoda volaná po kliknutí na tlačítko. Zjistí a zobrazí aktuální GPS polohu.
    /// Je 'async', protože získávání polohy může chvíli trvat a nechceme blokovat aplikaci.
    /// </summary>
    private async void OnZiskejPolohuClicked(object sender, EventArgs e)
    {
        // Blok 'try-catch' je zásadní pro ošetření chyb, které mohou nastat
        // při komunikaci s hardwarem nebo pokud chybí oprávnění.
        try
        {
            // Vytvoříme žádost, kde specifikujeme požadovanou přesnost a maximální dobu čekání.
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));

            // Klíčové volání, které se pokusí získat polohu. 'await' zajistí, že kód
            // počká na výsledek bez "zamrznutí" uživatelského rozhraní.
            Location location = await Geolocation.Default.GetLocationAsync(request);

            if (location != null)
            {
                // Pokud se polohu podařilo získat, zobrazíme souřadnice.
                // '\n' vytvoří nový řádek.
                LabelPoloha.Text = $"Zem. šířka: {location.Latitude}\nZem. délka: {location.Longitude}";
            }
            else
            {
                LabelPoloha.Text = "Polohu se nepodařilo zjistit.";
            }
        }
        // Každý 'catch' blok chytá specifický typ chyby (výjimky).
        catch (FeatureNotSupportedException fnsEx)
        {
            // Tato chyba nastane, pokud zařízení vůbec nemá GPS modul.
            await DisplayAlert("Chyba", "Vaše zařízení nepodporuje geolokaci.", "OK");
        }
        catch (PermissionException pEx)
        {
            // Nastane, pokud uživatel v dialogovém okně odmítl udělit oprávnění.
            await DisplayAlert("Chyba", "Aplikace nemá oprávnění k přístupu k poloze.", "OK");
        }
        catch (Exception ex)
        {
            // Tento blok zachytí všechny ostatní, neočekávané chyby (např. má-li uživatel vypnuté polohové služby).
            await DisplayAlert("Chyba", $"Vyskytla se chyba: {ex.Message}", "OK");
        }
    }

    /* ==================================================================================
     * LEKCE 3: Fotoaparát a Galerie
     *
     * Požadovaná oprávnění v AndroidManifest.xml:
     * - <uses-permission android:name="android.permission.CAMERA" />
     * - <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
     * ================================================================================== */

    /// <summary>
    /// Metoda volaná po kliknutí na tlačítko. Otevře galerii pro výběr obrázku a zobrazí ho.
    /// </summary>
    private async void OnVybratZGalerieClicked(object sender, EventArgs e)
    {
        try
        {
            // Otevře nativní rozhraní pro výběr fotky a čeká na akci uživatele.
            FileResult photo = await MediaPicker.Default.PickPhotoAsync();

            if (photo != null)
            {
                // Z bezpečnostních důvodů nám systém vrací jen dočasnou kopii souboru.
                // Proto si ji musíme zkopírovat do privátní složky naší aplikace.
                string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);

                // Otevřeme datový proud (stream) pro čtení z dočasné fotky.
                // 'using' zajistí, že se stream po použití automaticky uzavře.
                using Stream sourceStream = await photo.OpenReadAsync();

                // Otevřeme stream pro zápis do našeho nového souboru.
                using FileStream localFileStream = File.OpenWrite(localFilePath);

                // Zkopírujeme data ze zdrojového do cílového streamu.
                await sourceStream.CopyToAsync(localFileStream);

                // Až teď, když máme spolehlivou lokální kopii, ji zobrazíme.
                ImgFotka.Source = ImageSource.FromFile(localFilePath);

                // Uložíme si cestu k souboru pro pozdější sdílení.
                cestaKNahranemuObrazku = localFilePath;
                // A aktivujeme tlačítko pro sdílení.
                BtnSdiletObrazek.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Chyba", $"Nepodařilo se načíst fotku: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Metoda volaná po kliknutí na tlačítko. Otevře fotoaparát a pořízenou fotku zobrazí.
    /// </summary>
    private async void OnVyfotitClicked(object sender, EventArgs e)
    {
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                // Otevře nativní aplikaci fotoaparátu a čeká na pořízení snímku.
                FileResult photo = await MediaPicker.Default.CapturePhotoAsync();

                if (photo != null)
                {
                    // Zpracujeme a zobrazíme fotku (stejný postup jako u výběru z galerie).
                    string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                    using Stream sourceStream = await photo.OpenReadAsync();
                    using FileStream localFileStream = File.OpenWrite(localFilePath);
                    await sourceStream.CopyToAsync(localFileStream);
                    ImgFotka.Source = ImageSource.FromFile(localFilePath);

                    // Uložíme si cestu k souboru pro pozdější sdílení.
                    cestaKNahranemuObrazku = localFilePath;
                    // A aktivujeme tlačítko pro sdílení.
                    BtnSdiletObrazek.IsEnabled = true;
                }
            }
            else
            {
                await DisplayAlert("Nepodporováno", "Vaše zařízení nepodporuje pořizování fotek.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Chyba", $"Vyskytla se chyba: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// NOVÁ METODA: Je volána po kliknutí na tlačítko "Sdílet obrázek".
    /// Otevře nativní dialog pro sdílení a předá mu vybraný obrázek.
    /// </summary>
    private async void OnSdiletObrazekClicked(object sender, EventArgs e)
    {
        // Zkontrolujeme, zda máme uloženou cestu k nějakému obrázku.
        // string.IsNullOrEmpty je bezpečný způsob, jak ověřit, že textový řetězec není prázdný.
        if (string.IsNullOrEmpty(cestaKNahranemuObrazku))
        {
            await DisplayAlert("Chyba", "Nejdříve musíte vybrat nebo vyfotit obrázek.", "OK");
            return;
        }

        try
        {
            // Vytvoříme žádost o sdílení. Můžeme nastavit titulek, který se zobrazí v dialogu.
            // Na místo ShareFileRequest mohu pouižít ShareTextRequest (Text nebo Uri, Title), ShareMultipleFilesRequest...
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Sdílet můj obrázek",
                File = new ShareFile(cestaKNahranemuObrazku)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Chyba", $"Sdílení se nezdařilo: {ex.Message}", "OK");
        }
    }

    /* ==================================================================================
     * LEKCE 4: Kontakty a Volání
     *
     * Požadovaná oprávnění v AndroidManifest.xml:
     * - <uses-permission android:name="android.permission.READ_CONTACTS" />
     * A pro funkčnost volání na moderním Androidu je nutné přidat do manifestu za posledni uses-permission:
     * <queries>
     * <intent>
     * <action android:name="android.intent.action.DIAL" />
     * <data android:scheme="tel" />
     * </intent>
     * </queries>
     * ================================================================================== */

    /// <summary>
    /// Pomocná metoda, která zkontroluje stav oprávnění pro čtení kontaktů.
    /// Pokud oprávnění není uděleno, požádá o něj.
    /// </summary>
    /// <returns>Finální stav oprávnění po kontrole/žádosti.</returns>
    public async Task<PermissionStatus> CheckAndRequestContactsPermission()
    {
        PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.ContactsRead>();

        if (status == PermissionStatus.Granted)
            return status;

        // Toto volání zobrazí systémový dialog s žádostí o oprávnění.
        status = await Permissions.RequestAsync<Permissions.ContactsRead>();

        return status;
    }

    /// <summary>
    /// Metoda volaná po kliknutí na tlačítko. Vyžádá si oprávnění a otevře výběr kontaktu.
    /// </summary>
    private async void OnVybratKontaktClicked(object sender, EventArgs e)
    {
        try
        {
            // Nejdříve zkontrolujeme a případně požádáme o oprávnění.
            PermissionStatus status = await CheckAndRequestContactsPermission();

            // Pokud uživatel oprávnění neudělil, nebudeme pokračovat.
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Oprávnění vyžadováno", "Pro výběr kontaktu je nutné povolit přístup ke kontaktům.", "OK");
                return;
            }

            // Otevře nativní okno pro výběr kontaktu. Používáme plně kvalifikovaný název
            // pro zamezení případným konfliktům.
            var contact = await Microsoft.Maui.ApplicationModel.Communication.Contacts.PickContactAsync();

            if (contact != null)
            {
                // Uložíme si kontakt do proměnné pro pozdější použití při volání.
                vybranyKontakt = contact;

                // Zobrazíme jméno a první nalezené telefonní číslo.
                // 'FirstOrDefault()' bezpečně vrátí první prvek nebo 'null', pokud žádný není.
                // Operátor '?' (null-conditional) zajistí, že se kód nepokusí přistoupit
                // k 'PhoneNumber', pokud by byl výsledek 'FirstOrDefault()' null.
                LabelKontaktInfo.Text = $"Kontakt: {vybranyKontakt.DisplayName}\n" +
                                        $"Číslo: {vybranyKontakt.Phones.FirstOrDefault()?.PhoneNumber}";

                // Aktivujeme tlačítko pro volání.
                BtnVytocitCislo.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Chyba", $"Nepodařilo se načíst kontakty: {ex.Message}", "OK");
            BtnVytocitCislo.IsEnabled = false;
        }
    }

    /// <summary>
    /// Metoda volaná po kliknutí na tlačítko "Vytočit číslo".
    /// </summary>
    private async void OnVytocitCisloClicked(object sender, EventArgs e)
    {
        if (vybranyKontakt != null && vybranyKontakt.Phones.Any())
        {
            // Zkontrolujeme, zda zařízení podporuje funkci volání. Díky <queries>
            // v manifestu by toto mělo nyní fungovat správně.
            if (PhoneDialer.Default.IsSupported)
            {
                string cislo = vybranyKontakt.Phones.First().PhoneNumber;

                // Tato metoda nezačne ihned volat. Jen otevře nativní aplikaci
                // pro telefonování a předvyplní do ní naše číslo.
                PhoneDialer.Default.Open(cislo);
            }
            else
            {
                await DisplayAlert("Chyba", "Toto zařízení nepodporuje volání.", "OK");
            }
        }
    }

    /* ==================================================================================
     * LEKCE 5: Lokální Notifikace
     * Požadován NuGet balíček Plugin.LocalNotification
     * Požadovaná oprávnění v AndroidManifest.xml:
     * - <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
     * - <uses-permission android:name="android.permission.SCHEDULE_EXACT_ALARM" />
     * ================================================================================== */

    /// <summary>
    /// Zkontroluje stav oprávnění pro notifikace a podle toho upraví
    /// text a chování tlačítka pro notifikace. Byl totiž problém nastavit oprávnění a vytvořit notifikaci v jednom kroku.
    /// </summary>
    private async Task NastavTlacitkoNotifikace()
    {
        // Zkontrolujeme, zda jsou notifikace povoleny.
        if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
        {
            // Pokud ne, tlačítko bude sloužit k vyžádání oprávnění.
            BtnNotifikace.Text = "Povolit notifikace";
        }
        else
        {
            // Pokud ano, tlačítko bude sloužit k naplánování notifikace.
            BtnNotifikace.Text = "Naplánovat notifikaci";
        }
    }

    /// <summary>
    /// Metoda volaná po kliknutí na tlačítko "Notifikace".
    /// Vytvoří a naplánuje lokální notifikaci za 20 sekund.
    /// </summary>
    private async void OnNotifikaceClicked(object sender, EventArgs e)
    {
        // Znovu zkontrolujeme, jestli oprávnění máme, pro případ, že by ho uživatel mezitím v nastavení vypnul.
        if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
        {
            // Pokud oprávnění stále nemáme, požádáme o něj.
            if (await LocalNotificationCenter.Current.RequestNotificationPermission() == false)
            {
                // Uživatel oprávnění zamítl.
                await DisplayAlert("Oprávnění zamítnuto", "Pro zobrazení notifikace je nutné povolit oprávnění v nastavení telefonu.", "OK");
            }

            // Ať už uživatel oprávnění povolil nebo ne, znovu aktualizujeme stav tlačítka.
            // Pokud povolil, text se změní na "Naplánovat notifikaci".
            // Pokud nepovolil, text zůstane "Povolit notifikace".
            await NastavTlacitkoNotifikace();
            return; // Ukončíme metodu, nebudeme hned plánovat.
        }

        // --- Pokud se kód dostal až sem, OPRÁVNĚNÍ MÁME a můžeme plánovat ---
        try
        {
            string casVytvoreni = DateTime.Now.ToString("g");
            // Sestavíme "žádost" o notifikaci. Je to objekt, který nese všechny informace.
            var request = new NotificationRequest
            {
                // Unikátní (v rámci této aplikace) ID notifikace. Pokud bychom později chtěli notifikaci zrušit,
                // použili bychom toto ID. Pokud má aplikace více notifikací, každá by měla mít své unikátní ID, jinak novou notifikací přepíši tu původní
                NotificationId = 1337,

                // Text, který se zobrazí jako hlavní nadpis notifikace.
                Title = "Základy s Telefonem MAUI",

                // Podrobnější text notifikace.
                Description = $"Notifikace vytvořena v: {casVytvoreni}",

                // Podtitulek, který se může zobrazit na některých platformách.
                Subtitle = "Toto je lokální notifikace!",

                // Když uživatel na notifikaci klikne, vrátí se tato hodnota.
                // Můžeme ji později v aplikaci zachytit a reagovat na ni (např. otevřít specifickou stránku).
                // V našem případě to způsobí, že se aplikace otevře.
                ReturningData = "Dummy_Data",

                // Klíčová část: Plánování.
                // Nastavíme, že notifikace se má zobrazit za 20 sekund od teď.
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = DateTime.Now.AddSeconds(20)
                }
            };

        // Předáme naši žádost centru pro notifikace, které se postará o její zobrazení
        // v naplánovaný čas. Metoda Show vrací výsledek, zda se plánování podařilo.
        var result = await LocalNotificationCenter.Current.Show(request);

        // Informujeme uživatele, že notifikace byla úspěšně naplánována.
        await DisplayAlert("Naplánováno", $"Notifikace byla úspěšně naplánována a zobrazí se za 20 sekund. (ID: {result.ToString()})", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Chyba", $"Plánování notifikace se nezdařilo: {ex.Message}", "OK");
        }
    }

    /* ==================================================================================
     * LEKCE 6: Kompas
     *
     * Požadovaná oprávnění v AndroidManifest.xml:
     * žádná nejsou potřeba />
     * ================================================================================== */

    private void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
    {
        // 'e.Reading.HeadingMagneticNorth' obsahuje hodnotu ve stupních (0-360),
        // kde 0 je magnetický sever.
        double heading = e.Reading.HeadingMagneticNorth;

        // DŮLEŽITÉ: Události ze senzorů často přicházejí na vedlejším vlákně (ne na hlavním UI vlákně).
        // Jakékoliv změny v uživatelském rozhraní (jako změna textu nebo rotace)
        // se musí provádět na hlavním vlákně. K tomu slouží MainThread.BeginInvokeOnMainThread,
        // který zajistí, že kód uvnitř závorek se spustí na hlavním vlákně aplikace, až bude mít čas
        //bool jeNaHlavnimVlakne = MainThread.IsMainThread; - Takto bychom zjistili, zda jsme na hlavním vlákně.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Nastavíme rotaci naší střelky.
            LabelStrelka.Rotation = heading;

            // Zobrazíme číselnou hodnotu, zaokrouhlenou na 2 desetinná místa.
            LabelKompasHodnota.Text = $"Natočení: {heading:F2}°";
        });
    }
}
