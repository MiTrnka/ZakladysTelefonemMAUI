namespace ZakladysTelefonemMAUI;

// 'public partial class MainPage' definuje třídu pro hlavní stránku vaší aplikace.
// 'partial' znamená, že definice této třídy může být rozdělena do více souborů.
// V našem případě je druhá část generována na pozadí z XAML souboru.
// ': ContentPage' znamená, že MainPage dědí všechny vlastnosti a funkce od základní třídy ContentPage.
public partial class MainPage : ContentPage
{
    // Toto je privátní proměnná na úrovni celé třídy. Slouží k uchování informací
    // o kontaktu, který uživatel vybral. Je definována zde, aby byla dostupná
    // ve více metodách (v OnVybratKontaktClicked se naplní, v OnVytocitCisloClicked se použije).
    private Contact vybranyKontakt;

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
}