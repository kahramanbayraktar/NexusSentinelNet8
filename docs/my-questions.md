# Project Questions & Todo List

## Pending Questions

## Answered Questions

### General / CLI
- **Q:** kafka environment property'lerini anlamadım. comment'ler yetersiz. (done)
  - **A:** Bu konuyu `docker-compose.yml` içinde detaylı yorum satırlarıyla açıkladık. Ayrıca ileride gerekirse `docs/kafka-deep-dive.md` hazırlayabiliriz.

- **Q:** `dotnet new classlib -n NexusSentinel.Shared -o src/NexusSentinel.Shared` komutunda neden iki kez isim geçiyor? (done)
  - **A:** 
    - `-n`: Projenin **Mantıksal Adı** (.csproj ismi ve namespace).
    - `-o`: Projenin **Fiziksel Yolu** (Disk üzerindeki klasör).
    - İkisini ayırarak, projeyi ana dizin yerine `src/` altındaki özel bir klasöre koymayı başardık.

- **Q:** `.sln` dosyasındaki `Project(...) = "src"` nedir? `src` bir proje mi? (done)
  - **A:** Hayır, kod dosyası içeren bir proje değildir. Bu, Visual Studio'nun "Solution Folder" yapısıdır. Fiziksel diskteki `src` klasörünü, IDE içinde sanal bir grup olarak göstermek için kullanılır.

- **Q:** `project.nuget.cache` ne işe yarıyor? (done)
  - **A:** Bu geçici bir dosyadır (`obj` klasörü altında bulunur). `dotnet restore` komutu çalıştığında, hangi paketlerin yüklendiğini, versiyonlarını ve bunların nereden geldiğini buraya kaydeder. Bir sonraki derlemede "acaba paketleri tekrar kontrol etmeme gerek var mı?" sorusunun cevabını buradan hızlıca okur. `obj` klasörünü silersen tekrar oluşur, kritiktir ama kaynak kod değildir (git'e atılmaz).

- **Q:** Unix epoch time nedir? (done)
  - **A:** Bilgisayar dünyasında zamanın "milat" (başlangıç) noktasıdır: **1 Ocak 1970 00:00:00 UTC**. Bu tarihten itibaren geçen saniye (veya milisaniye) sayısını tutar.
    - *Avantajı:* Saat dilimi (Timezone) sorunu yoktur, sadece bir sayıdır (örn: `1707123456`). Hız ve depolama için idealdir.
    - *Örnek:* Şu anki zamanı "2026-02-05..." diye tutmak yerine `1770281234` gibi bir `long` sayı olarak tutarız.

- **Q:** `message TelemetryRecord` içindeki `device_id = 1;` gibi sayılar nedir? Değer ataması mı? (done)
  - **A:** Hayır, bunlar varsayılan değer değildir. Bunlar **Benzersiz Alan Etiketleridir (Field Tags)**.
    - Protobuf veriyi ağdan gönderirken "device_id" diye uzun bir metin göndermez. Sadece `1` numarasını ve değerini gönderir (Örn: `1: "Sensor-A"`).
    - *Kritik Kural:* Bu numaraları bir kez verdikten sonra **ASLA değiştirmemelisin**. Eğer `temperature = 2` iken yarın `temperature = 5` yaparsan, eski versiyonu kullanan cihazlar sistemi patlatır.
    - `1-15` arası numaralar en az yer kaplayanlardır (1 byte), bu yüzden en çok kullanılan alanlara verilir.

- **Q:** Bir önceki soruya ek: Bu sayıların verilmediği alanlar olabilir mi? (done)
  - **A:** Hayır, kesinlikle olamaz. Protobuf formatında her alanın (field) bir etiketi (tag number) olması **zorunludur**. Çünkü veriyi "isimle" değil bu "numarayla" eşleştirir. Numara yoksa Protobuf çalışmaz.

- **Q:** obj/debug klasörü altındaki Proto dosyalarının içeriğine hakim olmalı mıyım? Hangi kısımlarını bilmeliyim? Bu dosyaları manuel olarak kullanacak mıyım, yani düzenleme yapacak mıyım? (done)
  - **A:** 
    - **Düzenleme:** Asla! O dosyalar her `build` işleminde otomatik üretilir ve sıfırlanır. Oraya yazdığın her şey silinir.
    - **Hakimiyet:** İçeriğini okumana gerek yok (çok karışıktır). Sadece `telemetry.proto` içinde tanımladığın mesajların C# class'larına dönüştüğünü bilmen yeterli. Örneğin `TelemetryRecord` adında bir C# class'ın otomatik oluştuğunu bilip onu kullanacaksın.

- **Q:** Multiplexed streaming nedir? (done)
  - **A:** Tek bir fiziksel bağlantı (TCP bağlantısı) üzerinden, aynı anda birden fazla veri akışını birbirine karıştırmadan gönderebilme yeteneğidir.
    - *Analoji:* Tek bir otoyol şeridinden (TCP Connection) hem kırmızı arabaların (Akış A) hem de mavi arabaların (Akış B) sırayla gitmesi ama varışta ayrıştırılmasıdır.
    - *Farkı:* Eski REST/HTTP1.1'de her istek için yeni bağlantı açılırdı (pahalı). gRPC/HTTP2'de tek bağlantı sürekli açık kalır ve yüzlerce cihaz verisi bu tek borudan akar. Verimliliği sağlayan budur.

- **Q:** Bir önceki soruya ek: Yani çift yönlü anlamına gelmiyor? (done)
  - **A:** Tek başına "Multiplexing" çift yönlü demek değildir; sadece "çokluluk" demektir.
    - Ancak gRPC, HTTP/2 üzerinde çalıştığı için **hem Multiplexing hem de Bidirectional Streaming** (Çift Yönlü Akış) özelliklerine sahiptir.
    - Yani evet, aynı borudan (bağlantıdan) hem sen sunucuya veri basabilirsin hem de sunucu sana o an cevap dönebilir. (Tıpkı telefon görüşmesi gibi, Telsiz gibi sırayla değil).

- **Q:** ServerCallContext nedir? (done)
  - **A:** HTTP dünyasındaki `HttpContext`'in gRPC karşılığıdır.
    - İstemcinin gönderdiği Metadata'ya (Headerlar), kimlik bilgilerine (Auth), iptal taleplerine (`CancellationToken`) ve IP adresine buradan erişiriz.

- **Q:** `async Task<TelemetryAck> StreamTelemetry` üzerinden bana asenkron çalışmayı anlat. (done)
  - **A:** 
    - `async`: Metodun içindeki işlemlerin (örn: veri okuma) thread'i bloklamadan, arka planda yapılacağını belirtir.
    - `Task<TelemetryAck>`: "Sana hemen bir sonuç veremem çünkü akış ne zaman biter bilmiyorum, ama işim bitince söz sana bir `TelemetryAck` döneceğim" vaadidir.
    - `IAsyncStreamReader`: Klasik `List` gibi tüm verinin gelmesini beklemez. Musluktan su damlar gibi veri geldikçe (`await foreach`), sistem uyanır ve o veriyi işler. Veri yokken sistem uyur (CPU harcamaz).

- **Q:** `app.MapGrpcService<TelemetryIngestionService>()` tam olarak ne yapar? (done)
  - **A:** Yazdığın sınıfı Kestrel Web Sunucusu'na "Tanıtır" (Register eder).
    - Protobuf dosyasındaki paket ismine bakar (örn: `telemetry.TelemetryService`).
    - Gelen HTTP/2 isteklerinden URL'i `/telemetry.TelemetryService/StreamTelemetry` olanları yakalar ve senin `StreamTelemetry` metoduna yönlendirir (Routing).

- **Q:** IDE'de 2 farklı suggestion aracı görüyorum (Screenshots). Bu ikisi nedir? (done)
  - **A:**
    - **1. Liste (IntelliSense):** Klasik önericidir. Projendeki derlenmiş sınıfları, değişkenleri kural tabanlı listeler. Kesindir. (SS 1'deki alt alta liste).
    - **2. Hayalet Yazı (Ghost Text):** AI tabanlıdır (Copilot veya Visual Studio IntelliCode). Senin kod yazma alışkanlığına ve bağlama bakarak "Muhtemelen bunu yazacaksın" diye tahmin yürütür. (SS 2'deki silik gri yazı).

- **Q:** Grpc.Net.Client Grpc'nin Client package'ı ise, Server package'ı hangisi? Tools mu? Simulator bir Grpc Client değil mi? Evetse, neden Tools package'ını da oraya ekledik? (done)
  - **A:**
    - **Server Paketi:** `Grpc.AspNetCore`. (Ingestion servisinde bu vardı, çünkü o bir sunucu).
    - **Client Paketi:** `Grpc.Net.Client`. (Simülatör bir istemci olduğu için bunu ekledik).
    - **Tools Paketi:** `Grpc.Tools`. Bu çalışma zamanında (Runtime) BİR İŞE YARAMAZ. Sadece **Derleme Zamanı (Build Time)** aracıdır.
    - **Neden Ekledik?** Normalde `.proto` dosyaları `Shared` projesinde olduğu ve referans verdiğimiz için Simülatör'e eklemeyebilirdik. Ancak bazen IDE'ler (Intellisense) proto dosyalarını doğru taramak için bu araca ihtiyaç duyar. Teknik olarak `Shared`'dan referans aldığımız için zorunlu değildi, garanti olsun diye ekledik. İleride silebiliriz.

- **Q:** `DateTimeOffset.UtcNow.ToUnixTimeSeconds()` neden `DateTime`'da yok? (done)
  - **A:** `DateTime`, zaman dilimi (timezone) bilgisini net taşımaz; "hangi zamana göre saat 12?" sorusu muallaktır.
    - Unix Epoch, **UTC+0**'a göre hesaplanır. Bu yüzden bu metod, sadece zaman farkını kesin bildiği `DateTimeOffset` yapısına koyulmuştur. Hata yapmanı engellemek için `DateTime`'a koymamışlardır.

- **Q:** Kodları açıkla:
    `var streamCalls = client.StreamTelemetry();`
    `var streamWriter = streamCalls.RequestStream;`
    `await streamWriter.CompleteAsync();`
    `var response = await streamCalls;` (done)
  - **A:**
    1.  `client.StreamTelemetry()`: "Alo santral, ben bir çağrı başlatıyorum ama kapatma, konuşmam uzun sürecek" der. (Bağlantı açılır, ama cevap beklenmez).
    2.  `streamCalls.RequestStream`: O açık hattın "mikrofonunu" eline alırsın. Buradan konuşup veri göndereceksin.
    3.  `streamWriter.CompleteAsync()`: "Benim diyeceklerim bitti, konuşmamı sonlandırıyorum, tamam." dersin. Mikrofonu kapatırsın.
    4.  `await streamCalls`: "Eee, ne diyorsun anlattıklarıma?" diye karşı tarafın (sunucunun) son cevabını beklersin (Ack).

- **Q:** Kafka'yı neden kullanıyoruz? (done)
  - **A:** 
    - **Buffer (Şok Emici):** IoT cihazlarından saniyede binlerce veri gelirken, veritabanı veya işlemci servis yavaşlarsa sistem tıkanmasın diye. Kafka veriyi kuyrukta tutar, arkadaki servisler uygun oldukça işler (Backpressure).
    - **Decoupling (Bağımlılığı Koparma):** Ingestion servisi, veriyi kimin işlediğini bilmek zorunda değildir. Sadece Kafka'ya atar ve işine bakar.

- **Q:** Kafka'nın BootstrapServers parametresi ne işe yarar? (done)
  - **A:** Kafka kümesine (Cluster) ilk girişi sağlayan "Tanışma Noktasıdır".
    - İstemci (Producer/Consumer) bu adrese bağlanıp "Selam, kümede başka hangi sunucular var, lider kim?" diye sorar (Metadata Request).
    - Buradan aldığı harita ile diğer sunucularla konuşur. Yani tüm sunucuları tek tek yazmamıza gerek kalmaz.

- **Q:** "ProducerBuilder kullanarak producer'ı inşa etmek ve sisteme Singleton olarak eklemek. (Producer'lar thread-safe'tir, tek bir tane olması yeterlidir ve performanslıdır)."
Bu ifadeyi açar mısın? (done)
  - **A:**
    - **Producer Oluşturmak Pahalıdır:** Kafka ile bağlantı kurmak, metadata çekmek zaman ve kaynak harcar. Her veri geldiğinde `new Producer()` dersen sistem yavaşlar.
    - **Singleton (Tekillik):** "Uygulama boyunca SADECE BİR TANE Producer üret ve herkes onu kullansın" demektir.
    - **Thread-Safe (İplik Güvenli):** Aynı anda 100 farklı yerden (thread) bu tek producer nesnesini kullanıp veri gönderebilirsin; birbirlerinin işini bozmazlar, çakışma olmaz. Confluent.Kafka kütüphanesi bunu garanti eder.
    - *Sonuç:* Tek bir nesne ile binlerce isteği güvenle ve ışık hızında karşılarız.

- **Q:** "Performans Notu: Ingestion servisinde 'await' kullanmadan Produce yapmak (Fire-and-forget) daha hızlıdır. Ancak veri kaybı olmasın/garanti olsun diye ProduceAsync kullanımı da yaygındır. Şimdilik senkron Produce kullanıyoruz, arka planda bufferlanıp gider." Bu açıklamayı detaylandırabilir misin? (done)
  - **A:**
    - **Produce (Senkron/Fire-and-forget):** Sen tetiğe basarsın, veri kütüphanenin içindeki hafıza kuyruğuna (internal buffer) alınır. Metod hemen döner. Kafka broker'ından "Veri ulaştı" cevabı gelmesini beklemezsin. Çok hızlıdır ama o sırada elektrik kesilirse kuyruktaki veri uçabilir.
    - **ProduceAsync (Asenkron/Guaranteed):** Veriyi gönderir ve Kafka broker'ından "Tamam, diske yazdım" (Ack) yanıtı gelene kadar `await` ile beklersin. Daha güvenlidir ama her mesajda milisaniyelerce beklemek toplam performansı (throughput) düşürür.
    - **Neden Senkron Seçtik?** IoT verisi genelde çok yoğundur. Arka planda kütüphane bu verileri paketleyip (batching) topluca gönderir, bu da verimliliği artırır. Birkaç verinin kaybı göze alınabilirse bu yöntem tercih edilir.

- **Q:** sync = fire-and-forget, async = wait-and-confirm. Böyle diyebilir miyiz? Ve her zaman böyle midir? (done)
  - **A:**
    - **Kafka Özelinde:** Evet, `Produce` metodunu fire-and-forget, `ProduceAsync` metodunu wait-and-confirm gibi düşünebilirsin.
    - **Genel Programlamada:** Genellikle tam tersidir! Normalde `Sync` (senkron) bir işlem baka baka beklemek (blocking) demektir. `Async` (asenkron) ise "sen işine bak, bitince bana haber ver" demektir. 
    - **İstisna:** Kafka kütüphanesi performansı artırmak için `Produce` metodunu "belleğe yazıp hemen dönen" (non-blocking) bir yapıda kurmuş. Bu yüzden kavramlar burada biraz kafa karıştırabilir.

- **Q:** docker-compose.yml dosyasının ana dizinde bir klasörde olması sorun oluyor mu? Direkt ana dizinde olması gerekmiyor mu? (done)
  - **A:** Sorun olmaz, hatta büyük projelerde "Infrastructure" veya "Docker" klasörü altında tutmak tertemiz bir `root` dizini sağlar.
    - **Dikkat:** Sadece terminalde komutu çalıştırırken o klasöre girmeli veya `docker-compose -f docker/docker-compose.yml up` şeklinde dosya yolunu göstermelisin. Proje içindeki servisler (C# kodları) zaten `localhost` üzerinden bağlandığı için dosyanın nerede olduğundan etkilenmezler.


- **Q:** Console app ile Worker app arasındaki farklar neler? Bu projede Redis katmanı için neden Worker app kullandık? Detaylı anlat. (done)
  - **A:**
    - **Console App:** "Başla ve Bitir" işleri için idealdir (örn: script çalıştırmak, veri taşımak). Basittir ama sürekli çalışan bir servis olmak için ekstra kod (döngüler, hata yönetimi) yazman gerekir.
    - **Worker Service:** "Sürekli Çalışan Arka Plan Hizmeti"dir. İçinde Dependency Injection, Logging, Configuration (appsettings) hazır gelir. Linux'ta Daemon, Windows'ta Service olarak çalışmaya doğuştan yeteneklidir.
    - Bu servis hiç durmadan 7/24 Kafka dinleyecek, Console şablonu buna yetersiz kalırdı.

- **Q** Kafka ayarlarındaki GroupId nedir? (done)
  - **A:** Kafka'nın en güçlü özelliklerinden biri olan "Consumer Group" mekanizmasının kimliğidir.
    - **Yük Dağılımı (Scaling):** Eğer aynı `GroupId` ile 3 farklı Processor çalıştırırsan, Kafka gelen verileri bu 3 Processor'a *paylaştırır*. Yani her biri verinin %33'ünü işler.
    - **Yayıncılık (Broadcasting):** Eğer *farklı* `GroupId` verirsen (örneğin biri "ProcessorGroup", diğeri "ArchiveGroup"), Kafka verinin *kopyasını* her gruba ayrı ayrı gönderir.
    - **Kaldığı Yer (Offset):** Sistem çökerse, Kafka bu `GroupId`'nin nerede kaldığını hatırlar. Geri geldiğinde işlenmemiş veriden devam eder.

- **Q** Bir önceki soruya ek: Bu Processor dediğimiz şey tam olarak nedir? Bilgisayarın fiziksel işlemcisi midir? Yoksa bir yazılım mıdır? (done)
  - **A:** Kesinlikle bir **Yazılım (Software)** parçasıdır.
    - Fiziksel işlemci (CPU) donanımdır. Bizim kodumuz olan "Processor Service", bu donanımı kullanarak veriyi işleyen bir Microservice'dir. Adının "Processor" olması, veriyi alıp, işleyip (process), dönüştürmesinden gelir.

- **Q** .NET'te program.cs dosyalarında gördüğümüz builder ve app değişkenleri neyi temsil eder? Her bir app tipi için (Console, Worker, Web API) anlat. (done)
  - **A:**
    - **Builder (İnşaat Şantiyesi):** Binayı yapmadan önce malzemeleri (Config) ve işçileri (Services/DI) topladığımız yerdir.
      - *Worker/Console:* `HostApplicationBuilder`. Web özellikleri yoktur, hafiftir.
      - *Web API:* `WebApplicationBuilder`. Ekstra olarak portları, sunucu ayarlarını (Kestrel) bilir.
    - **App / Host (Bitmiş Bina):** `builder.Build()` dediğimizde şantiye biter, bina ortaya çıkar. `Run()` dediğimizde kapılar açılır.
      - *Worker/Console:* `IHost`. Sadece arka planda çalışır.
      - *Web API:* `WebApplication`. Gelen HTTP isteklerini karşılayan kapıları (Middleware) vardır.

- **Q** InvalidOperationException ne zaman tercih edilir? (done)
  - **A:** Bir nesnenin veya sistemin **"şu anki durumu"** o işlemi yapmaya uygun olmadığında kullanılır.
    - *Örnek:* Araba boş vitesteyken gaza basarsan sorun yok, ama motor *kapalıyken* gaza basarsan `InvalidOperationException` alırsın.
    - *Bizim Durum:* "Redis Connection String yok" demek, uygulamanın çalışması için gereken temel durum bozuk demektir. Bu bir parametre hatası (`ArgumentException`) değil, sistemin genel halinin hatasıdır.

- **Q** "builder.Services.AddHostedService<Worker>();" Bu kodu neden yazıyoruz? Zaten bir Worker projesi oluşturduk? Worker ile pipeline neden zaten entegre değil? (done)
  - **A:**
    - **Projeyi Oluşturmak Yetmez:** Proje şablonu sadece dosya yapısını kurar. .NET Framework, `Worker` sınıfının varlığından habersizdir. Ona "Bak elimde böyle bir sınıf var, bunu al ve çalıştırmaya başla" emrini bu kodla veririz.
    - **Dependency Injection (DI) Nedir?** `builder.Services.Add...` demek, "Alet çantasına (Container) bir alet koymak" demektir.
      - **AddTransient:** Her isteyene **YENİ** bir tane ver. (Hafif, çerezlik nesneler).
      - **AddScoped:** Her HTTP isteği (Request) için **BİR** tane ver. (Veritabanı bağlantıları).
      - **AddSingleton:** Uygulama ölene kadar **TEK** bir tane yarat ve herkese onu ver. (Cache, Ayarlar).
      - **AddHostedService:** Bu özel bir Singleton'dır. Uygulama başlarken otomatik olarak `StartAsync` tetiklenir ve uygulama kapanana kadar çalışır. Arka plan işçileri için tek yol budur.


- **Q:** async bir metodu çağırırken neden await kullanıyoruz? Zaten async olarak tanımladık? (done)
  - **A:**
    - **Async (Tanım):** `async` kelimesi sadece "Bu metodun içinde bekleme (`await`) yapılabilir" ve "Geriye sonuç yerine bir `Task` (Gelecek Vaadi) döneceğim" garantisidir. Tek başına metodu sihirli bir şekilde farklı bir evrende çalıştırmaz.
    - **Await (Eylem):** `await` komutu ise **"Bu işlem bitene kadar buradaki akışı durdur, thread'i (iş parçacığını) boşa çıkar, sonuç gelince kaldığın yerden devam et"** demektir.
    - **Kullanmazsan Ne Olur?** Eğer `await` yazmazsan, kod o satırda işin bitmesini beklemez, *hemen* bir sonraki satıra geçer (Fire-and-forget). Arka plandaki iş bitmeden program ilerler, sonuç alamazsın ve hata oluşursa haberin bile olmaz (Exception Swallowing).
    - **Özet:** `async` yeteneği ("Ben bekleyebilirim") tanımlar, `await` ise o yeteneği ("Hadi bekle") kullanır.


- **Q:** consumer değişkenini using ile tanımlamış olmamıza rağmen neden finally bloğunda consumer.Close() çağırıyoruz? (done)
  - **A:**
    - Normalde `using` bloğu bitince `Dispose()` çalışır ve kaynaklar serbest bırakılır. Bu genel kuraldır ve doğrudur.
    - **Kafka Farkı:** Kafka, TCP protokolü üzerinden sürekli açık ve canlı bir bağlantı tutar. `Dispose()` metodu bu bağlantıyı "çat" diye kesebilir (Hard Kill).
    - **Close() Ne Yapar?** `Close()` metodu daha "naziktir" (Graceful Shutdown). Önce sunucuya "Ben gruptan ayrılıyorum" der (`LeaveGroup` isteği), grubun dengelenmesini (Rebalance) tetikler, bekleyen son offset'leri commit eder.
    - **Analoji:** `Dispose()` fişi prizden çekmekse, `Close()` Windows'u "Bilgisayarı Kapat" menüsünden kapatmaktır. Veri bütünlüğü ve grup sağlığı için önce `O(1)` sürede `Close`, sonra `Dispose` (using sayesinde otomatik) önerilir.

- **Q:** C#'taki primary constructor konseptini anlat. (done)
  - **A:**
    - C# 12 ile gelen, sınıfın tepesinde parametre tanımlayarak "Basmakalıp Kodları" (Boilerplate) azaltan özelliktir.
    - **Eski Stil:** Constructor metodu aç, parametre al, bunları yukarıda tanımladığın `private readonly` alanlara (field) tek tek elle eşle (`this._logger = logger` vs).
    - **Primary Constructor:** Sınıf isminin yanına parantez açıp `(ILogger logger, IConfiguration config)` yazarsın. Bitti! Artık o `logger` ve `config` tüm sınıf gövdesinde (hatta metodlarda değil, field initalization kısımlarında) erişilebilir olur.
    - **Senin Kodunda:** `Worker.cs` dosyasında `public class Worker(ILogger<Worker> logger...) : BackgroundService` diyerek bunu kullandık. Kod kalabalığını %40 azaltır ve daha okunaklı yapar.

- **Q:** docker-compose -f docker/docker-compose.yml up --build -d processor
  Parametreleri açıkla. (done)
  - **A:**
    - `-f docker/docker-compose.yml`: **File (Dosya).** Standart `docker-compose.yml` yerine başka bir dosya kullanacağımızı belirtir. Dosyamız `docker` klasöründe olduğu için bunu belirttik.
    - `up`: Servisleri oluştur ve başlat.
    - `--build`: Başlatmadan önce image'ları **yeniden derle**. (Kod değiştirdiğinde şarttır).
    - `-d`: **Detached (Ayrık).** Konteyneri arka planda çalıştır. Terminali kilitleme, bana geri ver.
    - `processor`: Tüm seti değil, **sadece** `processor` servisini (ve bağımlılıklarını) ayağa kaldır.

- **Q** .dockerignore kullanımının amacı nedir? Kullanılmazsa ne olur? (done)
  - **A:**
    - **Amaç:** Docker'ın "Build Context" (Derleme Bağlamı) aşamasında, gereksiz dosya ve klasörlerin (git geçmişi, derlenmiş yerel dosyalar, IDE ayarları) Docker motoruna kopyalanmasını engellemektir.
    - **Kullanılmazsa Ne Olur?**
      1.  **Hız:** Docker, `bin/obj` veya `.git` gibi devasa klasörleri arka plana kopyalamaya çalışır. Build süresi saniyelerden dakikalara (veya buradaki gibi 40 dakikaya) çıkar.
      2.  **Boyut:** Oluşan image boyutu gereksiz büyür.
      3.  **Güvenlik:** `.env` gibi hassas dosyalar yanlışlıkla image içine kopyalanabilir.

- **Q** docker-compose -f docker/docker-compose.yml up --build -d processor
    Bu komutu açıkla. (done)
    - **A:**
      - `-f docker/docker-compose.yml`: **File (Dosya).** Standart `docker-compose.yml` yerine başka bir dosya kullanacağımızı belirtir. Dosyamız `docker` klasöründe olduğu için bunu belirttik.
      - `up`: Servisleri oluştur ve başlat.
      - `--build`: Başlatmadan önce image'ları **yeniden derle**. (Kod değiştirdiğinde şarttır).
      - `-d`: **Detached (Ayrık).** Konteyneri arka planda çalıştır. Terminali kilitleme, bana geri ver.
      - `processor`: Tüm seti değil, **sadece** `processor` servisini (ve bağımlılıklarını) ayağa kaldır.

- **Q** `docker logs -f nexus-processor` vs `docker-compose -f docker/docker-compose.yml logs -f processor`
  - **A:**