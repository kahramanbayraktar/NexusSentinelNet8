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

- **Q** `docker logs -f nexus-processor` vs `docker-compose -f docker/docker-compose.yml logs -f processor`. Farkları ne? (done)
   - **A:**
     - `docker logs`: Doğrudan **Container ID** veya **Container Name** ile çalışır. Docker'ın kendi yerel komutudur. Hızlıdır ama container ismini tam bilmen gerekir (`nexus-processor`).
     - `docker-compose logs`: **Servis İsmi** ile çalışır (`processor`). docker-compose dosyasındaki mantıksal ismi kullanır. Arkada gidip container ID'sini kendi bulur.
     - *Fark:* Çalışma prensipleri aynıdır (ikisi de stdout okur). Compose versiyonu, proje bağlamında çalışırken ("processor servisine bak") daha rahattır; Docker versiyonu ise debug yaparken ("şu spesifik container'a bak") daha hızlıdır.

- **Q** `dotnet new blazor -o src/NexusSentinel.Dashboard -n NexusSentinel.Dashboard --interactivity Server`
bu komuttaki --interactivity Server parametresini açıkla. (done)
   - **A:**
     - Blazor'ın .NET 8 ile gelen "Render Mode" seçeneğidir.
     - **Server:** Tüm UI (Butonlar, tıklamalar) sunucuda işlenir. Tarayıcı ile sunucu arasında bir **SignalR (WebSocket)** tüneli açılır. Sen butona bastığında sinyal sunucuya gider, C# kodu çalışır, DOM güncellemesi geri gelir.
     - **Avantajı:** Yükleme süresi çok hızlıdır (HTML gelir). SEO dostudur. Veritabanına/Redis'e doğrudan erişebilirsin (API yazmana gerek kalmaz).
     - **Diğer Seçenek (WebAssembly):** Kodun DLL'leri tarayıcıya iner ve orada çalışır. İlk yükleme yavaştır ama sunucuya yük binmez.
     - *Bizim Seçimimiz:* Dashboard veriyi Redis'ten okuyacağı için, sunucu tarafında çalışan ve Redis'e direkt erişen "Server" modu en performansı ve kolay yöntemdir.

- **Q** Bir önceki soruyu daha detaylı cevapla. (done)
  - **A:**
    - **Blazor Nedir?** Microsoft'un modern web arayüzleri oluşturmak için geliştirdiği bir framework'tür. C# ve .NET ekosistemini kullanarak tarayıcıda çalışan zengin kullanıcı arayüzleri (UI) oluşturmanızı sağlar. JavaScript yazmadan, bildiğiniz C# diliyle interaktif web uygulamaları geliştirebilirsiniz.
    - **Blazor Server Modu Nedir?**
      - **Çalışma Prensibi:** Uygulamanın kullanıcı arayüzü (UI) tarayıcıda değil, sunucuda çalışır. Tarayıcı ile sunucu arasında sürekli açık duran bir **SignalR (WebSocket)** bağlantısı kurulur.
      - **İnteraksiyon:** Kullanıcı bir butona tıkladığında veya bir sayfayı değiştirdiğinde, bu olay tarayıcıdan sunucuya bir mesaj olarak gider. Sunucudaki C# kodu bu olayı işler, veritabanını veya Redis'i sorgular, gerekli hesaplamaları yapar ve ardından sadece değişen HTML parçalarını (DOM diff) tarayıcıya geri gönderir. Tarayıcı bu parçaları ekrana yansıtır.
      - **Avantajları:**
        1.  **Hızlı İlk Yükleme:** Tarayıcıya sadece HTML ve CSS yüklenir. JavaScript kütüphaneleri indirilmez, bu da sayfanın çok hızlı açılmasını sağlar.
        2.  **Güvenlik:** Hassas iş mantığı ve veritabanı bağlantıları sunucuda kalır. Tarayıcıya sadece sonuçlar gider, kodunuz veya verileriniz exposed olmaz.
        3.  **Basitlik:** API yazma ve frontend-backend iletişimi için ek katmanlar kurma ihtiyacı azalır. Doğrudan C# ile her şeye erişebilirsiniz.
        4.  **Real-time:** SignalR entegrasyonu sayesinde sunucudan anlık veri akışı (push) çok kolaydır.
      - **Dezavantajları:**
        1.  **Sunucu Yükü:** Her kullanıcı için sunucuda bir bağlantı ve bellek tutulması gerekir. Çok yüksek kullanıcı sayılarında sunucu kaynakları yetersiz kalabilir.
        2.  **Bağımlılık:** Kullanıcının internet bağlantısının sürekli ve stabil olması gerekir. Bağlantı koparsa uygulama çalışmaz.
        3.  **Gecikme (Latency):** Her etkileşim için sunucuya gidiş-dönüş yapıldığı için, çok uzak mesafelerdeki kullanıcılar için küçük gecikmeler hissedilebilir.
    - **Neden Bizim Projemiz İçin Uygun?**
      - **Veri Kaynağı:** Verilerimizi **Redis**'ten okuyoruz. Redis, sunucuya çok yakın (genellikle aynı container ağında) çalışan bir in-memory veritabanıdır. Bu nedenle Redis'e erişim çok hızlıdır ve gecikme neredeyse sıfırdır.
      - **Kullanıcı Sayısı:** Başlangıçta yüzlerce, belki binlerce eş zamanlı kullanıcı olmayacak. Bu nedenle sunucu kaynaklarının yetersiz kalma riski düşüktür.
      - **Geliştirme Kolaylığı:** API katmanı yazmadan doğrudan Redis'ten veri çekip ekranda göstermek, geliştirme sürecini çok hızlandırır.
      - **Real-time İhtiyacı:** Dashboard'da anlık veri güncellemeleri istiyoruz. Blazor Server, SignalR ile bunu doğal olarak destekler.
    - **Alternatif: Blazor WebAssembly (WASM)**
      - **Çalışma Prensibi:** Uygulamanın tüm C# kodu (DLL'ler) tarayıcıya indirilir ve orada, tarayıcının WebAssembly motoru üzerinde çalışır.
      - **Avantajı:** Sunucuya yük binmez, tamamen client-side çalışır. İnternet kesilse bile uygulama çalışmaya devam eder.
      - **Dezavantajı:** İlk yükleme çok daha yavaştır çünkü tüm kodun indirilmesi gerekir. Ayrıca, tarayıcıda çalışan kod olduğu için hassas verilerin sunucuda tutulması gerekir (API zorunlu hale gelir).
    - **Sonuç:** NexusSentinel projesinde, verilerin Redis'te olması ve geliştirme kolaylığı nedeniyle **Blazor Server** modu en mantıklı ve performanslı seçimdir.

- **Q** `builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConn));`
Neden Singleton? Detaylı açıkla. (done)
  - **A:**
    - `Singleton`: Uygulama ömrü boyunca **TEK BİR TANE** oluşturulur ve herkese o verilir.
    - **Neden Redis İçin?** Redis bağlantısı (`ConnectionMultiplexer`) kurmak pahalı bir işlemdir (TCP Handshake, Auth vb.).
    - Bu nesne, .NET tarafında **Thread-Safe** (İplik Güvenli) olarak tasarlanmıştır. Yani tek bir bağlantı üzerinden binlerce paralel isteği (Multiplexing) hatasız yönetebilir.
    - Eğer `Scoped` veya `Transient` yapsaydık, her gelen istekte yeni bağlantı açıp kapatırdık; bu da hem sunucuyu hem de Redis'i yorar, performansı öldürürdü.

- **Q** `builder.Services.AddScoped<DeviceStateService>();`
Neden Scoped veya Singleton? Detaylı açıkla. (done)
  - **A:**
    - **Neden Scoped?** Genellikle Blazor Server/Web API servisleri `Scoped` yapılır. Blazor Server'da `Scoped`, kullanıcının **oturumu (Circuit)** boyunca yaşar.
    - Yani Ahmet Bey siteye girdiğinde bir `DeviceStateService` oluşur, çıkana kadar o kullanılır. Mehmet Bey girdiğinde ona ayrı bir tane oluşur.
    - **Neden Singleton Yapmadık?** Aslında bu servis *state* (durum) tutmadığı için Singleton da olabilirdi. Ancak ilerde kullanıcıya özel filtreleme (örn: "Sadece benim cihazlarımı getir") eklersek, `User` bilgisine ihtiyaç duyarız. `User` bilgisi Scoped (Oturum bazlı) olduğu için, servisimiz de Scoped olmak zorunda kalır. Şimdiden Scoped yapmak güvenli bir standarttır.

- **Q** `if (!json.IsNullOrEmpty) { var record = JsonSerializer.Deserialize<TelemetryRecord>(json!); }`
    `json!` demeye gerek olmamalı diye düşünüyorum. ama onu koymayınca warning çıkıyor. Neden? (done)
  - **A:**
    - `RedisValue` (`json` değişkeni) bir `struct` türüdür. `JsonSerializer`, parametre olarak saf `string` bekler.
    - `RedisValue` -> `string` dönüşümü (implicit conversion) arka planda otomatik yapılır ama sonuç `string?` (yani null olabilir) döner.
    - **Derleyicinin Derdi:** Derleyici, senin `!json.IsNullOrEmpty` kontrolü yaptığını görüyor ama bu kontrolün, `implicit operator` sonucunu da `null` olmaktan kurtardığını **garanti edemiyor** (Static Analysis kısıtı).
    - "Ben null olmadığını kontrol ettim, sen rahat ol" demek için `!` (Null-forgiving operator) kullanıyoruz. Alternatif olarak `json.ToString()!` de diyebilirdik.

- **Q** şunu anlayamadım: docker/docker-compose.yml dosyasında bu servisleri yazmış ve çalıştırmıştık. o neydi ve şimdi yapmamı istediğin şey ne? yani docker-compose.yml yetersiz mi?? neden her bir servis için dockerfile oluşturmamı istiyorsun?
- **A:** 1. "Biz bunları eklememiş miydik?" Hayır, Phase 2 (Ingestion) ve Phase 3 (Processor) sırasında sadece Processor servisini Docker içine aldık.
Ingestion Service ve Simulator'ü bugüne kadar hep bilgisayarın yerel terminalinden (dotnet run diyerek) çalıştırdık.
kafka, redis, zookeeper gibi altyapı servislerini ise docker-compose.yml ile hazır (internet'ten indirilen) imajlar olarak çalıştırdık.
2. "Neden her biri için Dockerfile lazım? docker-compose yetmez mi?" 
docker-compose.yml , bir Orkestra Şefi'dir. Müzisyenlere (servislere) "Sen şurada otur, sen şu nota çal" der.

Ama ortada bir sorun var:

Redis, Kafka gibi servisler, "Hazır Müzisyenlerdir". Docker Hub'dan image: redis diyip indirir ve oturtursun.
Senin Yazdığın Kodlar (Ingestion, Dashboard) ise "Eğitimsiz Çıraklardır". Docker bunları tanımaz. Onları Docker'ın anlayacağı birer "Müzisyen" (Image) haline getirmek gerekir.
İşte Dockerfile, o çırağı alıp, giydirip, eline enstrüman verip sahneye çıkmaya hazır hale getiren **"Eğitim Kılavuzu"**dur (Build Recipe).
Eğer Dockerfile yazmazsak, docker-compose sadece hazır imajları (Redis, Kafka) çalıştırabilir ama senin C# kodunu çalıştıramaz.

Özetle:
Kod senin bilgisayarında (bin/debug) -> dotnet run ile çalışır.
Kod Docker'ın içinde (container) -> Dockerfile ile paketlenip, docker-compose ile çalıştırılır.

- **Q**  cache aşağıdaki adımların hangisinde devreye giriyor?
RUN dotnet restore "src/NexusSentinel.Ingestion/NexusSentinel.Ingestion.csproj"
RUN dotnet build "NexusSentinel.Ingestion.csproj" -c $BUILD_CONFIGURATION -o /app/build

- **A:** dotnet restore adımında devreye girer. Neden? Docker katmanlı çalışır. Restore komutundan önce sadece .csproj  dosyalarını kopyaladığımız için, yeni bir kütüphane eklemediğin sürece Docker bu adımı pas geçer (Cache'ten kullanır).
dotnet build adımında ise devreye girmez (bozulur), çünkü hemen öncesinde COPY . . ile tüm kaynak kodları kopyalarız. Kodda bir harf bile değişse cache geçersiz olur ve build yeniden çalışır.

- **Q** cache'in çalışması nedir tam olarak?
senaryo 1: hiç package eklemedim/güncellemedim. ne oluyor?
senaryo 2: bir package ekledim. ne oluyor?

- **A:** Senaryo 1 (Değişiklik Yok): Docker .csproj dosyalarının "parmak izine" (hash) bakar. Değişmediğini görünce dotnet restore adımını hiç çalıştırmaz, önceki derlemeden kalan hazır katmanı (cache) şipşak yapıştırır. Çok hızlıdır.

Senaryo 2 (Paket Eklendi): .csproj dosyasının parmak izi değişir. Docker "bu katman bozulmuş" der ve o satırdan itibaren cache'i çöpe atar. dotnet restore komutunu baştan çalıştırıp tüm paketleri internetten tekrar indirir. Süre uzar.

- **Q** senaryo 2'de package'ların tümü mü yeniden indirilir? yoksa sadece değişmiş/yeni eklenmiş olanlar mı?

- **A:** Tümü yeniden indirilir. Docker katman tabanlı bir sistemdir; .csproj dosyası üzerinde yapılan bir değişiklik olduğunda Docker o satırdaki (ve sonraki) tüm cache'i geçersiz sayar. dotnet restore komutu o "temiz" katmanda en baştan çalışır ve NuGet paket bazlı bir "sadece yenileri al" ayrımı yapamaz.

- **Q** Package kontrolü hangi dosyada yapılıyor? `.csproj` mu? (done)
- **A:** Evet, `.csproj` dosyası içindeki `<PackageReference>` etiketleri üzerinden yapılır. `dotnet restore` komutu bu dosyaya bakarak gerekli kütüphaneleri indirir.

- **Q** Neden `WORKDIR "/src/src/NexusSentinel.IoTSimulator"`? `/src` yeterli değil mi? (done)
- **A** 
  1. `WORKDIR /src`: Konteyner içinde `/src` klasörünü oluşturur ve oraya yerleşir.
  2. `COPY . .`: Bilgisayardaki tüm dosyaları bu klasöre kopyalar. Bilgisayarımızda kodlar zaten bir `src` klasörü içinde olduğu için, konteyner içindeki yapı `/src (workdir) / src (kopyalanan klasör)` şeklinde katmerli (nested) olur.
  3. `WORKDIR` mutlak mı?: Eğer `/` ile başlıyorsa konteynerin kök dizinden itibaren arar. Başlamıyorsa (relative) o anki klasörün içinden devam eder.

- **Q** Bir önceki soruya ek: Yani "src/NexusSentinel.IoTSimulator" da diyebilir miydik? (/ ile başlatmadan) (done)
- **A** Evet, diyebilirdik. `WORKDIR` komutu, eğer yol `/` ile başlamıyorsa (relative path), bir önceki `WORKDIR` klasörünün içinden devam eder. Dolayısıyla `/src` içindeyken `src/NexusSentinel.IoTSimulator` yazmak bizi `/src/src/NexusSentinel.IoTSimulator` adresine götürür.


- **Q** package kontrolü hangi dosyada yapılıyor? .csproj mu?
- **A** Evet, .csproj dosyası içindeki <PackageReference> etiketleri üzerinden yapılır. dotnet restore komutu bu dosyaya bakarak gerekli kütüphaneleri indirir.

- **Q** "gRPC portlarını dışarı açmıyoruz, sadece container ağı içinde haberleşecekler" ne demek? (done)
- **A** Docker içinde her konteyner kendi özel IP'sine ve ismine (DNS) sahip küçük bir bilgisayar gibidir. 
  1. **İçeriden (Container Network):** `Simulator` konteyneri, `Ingestion` konteynerine sadece ismiyle (`http://ingestion:8080`) ulaşabilir. Bunun için portun senin gerçek bilgisayarına (host machine) bağlanmasına gerek yoktur. Konteynırlar kendi aralarında "fısıldaşarak" konuşabilirler.
  2. **Dışarıdan (Host/Dış Dünya):** Eğer sen tarayıcıdan veya Postman'den `localhost:8080` diyerek o servise ulaşmak istersen, o zaman `ports:` kısmında kapıyı dışarıya (bilgisayarına) açman (map etmen) gerekir. 
  gRPC iletişimi sadece iki servis arasında kalacağı ve dışarıdan bir müdahale gerekmediği için gereksiz yere dışarıya kapı açmıyoruz. Bu hem güvenlik hem de port kalabalığını önlemek için iyidir.

- **Q** İki konteyner da içerde 8080 kullanırsa çakışmaz mı? (done)
- **A** Hayır. Her konteyner **ayrı bir bilgisayar** gibidir. İki farklı evde oda numarası 101 olan bir oda olması çakışma yaratmaz. Çakışma sadece **Host (senin bilgisayarın)** tarafında aynı portu (sol taraf) iki kez kullanmaya çalışırsan olur.

- **Q** şunu mu demek istiyorsun?: bir host/OS/PC port'u vardır (5000, 50003 gibi unique), bir de her bir container'ın kendi içindeki X uygulamasına ulaşmak için kullandığı bir port (8000, 8080 vb) vardır.
her bir port'un unique identifier'ı zaten vardır (dockerfile'daki section name ile belirlenir: ingestion/dashboard vb -> http://ingestion:8080 / http://dashboard:8080).
eğer bir container içinde birden fazla app olsaydı :'ın sağındaki portun unique olmasına dikkat etmemiz gerekirdi. şu an her bir app ayrı/kendi container'ına sahip.

- **A** Aynen öyle! 100% doğru anlamışsın. Özetlediğin bu mantık Docker'ın temelidir:
Unique Host Port: Senin PC'ndeki giriş kapısı (5000, 5003) unique olmalı.
Internal Port: Her konteynırın kendi içindeki kapı (8080) izoledir, diğerlerini etkilemez.
Service Discovery: Konteynır isimleri (ingestion, dashboard) Docker ağındaki unique kimliklerdir.
İzolasyon: Her uygulama kendi konteynırında olduğu için iç port çakışması imkansızdır.

- **Q** ConnectionStrings__Redis=redis:6379'daki 6379 aşağıdaki tanımdaki hangi porta (sol/sağ) denk gelmektedir?
ports: 
      - "6379:6379"

- **A** SAĞ (Sağdaki 6379) tarafa denk gelir.

Kural: Konteynerler kendi aralarında konuşurken her zaman birbirlerinin İç Portuna (Sağdaki değer) bağlanırlar.

Sol (6379): Senin bilgisayarından (Postman/Redis Desktop Manager vb.) bağlanman içindir.
Sağ (6379): Diğer konteynerlerin (Dashboard, Processor) bağlanması içindir.
Sol tarafı 9999:6379 yapsaydın bile, Dashboard konteyneri içeriden hala redis:6379 diyerek bağlanmaya devam edecekti.

- **Q** docker logs -f nexus-simulator komutundaki -f nedir?
- **A** -f parametresi, "follow" anlamına gelir. Bu komut, konteynerin loglarını canlı olarak izlemenizi sağlar. Yani, konteyner yeni bir log ürettikçe, terminal ekranında anında görünür.

- **Q** docker logs -f nexus-simulator komutunu çalıştırdığımda "Unhandled exception. System.InvalidOperationException: Cannot read keys when either application does not have a console or when console input has been redirected. Try Console.Read." hatası alıyorum. Ne yapmalıyım?
- **A** Bu hata, Docker'ın log izleme modunda (follow mode) çalışırken klavye girdisi (Console.ReadKey) okumaya çalışmasından kaynaklanır. Docker konteynerleri genellikle "headless" (konsolsüz) çalışır.

Çözüm: 

1. Uygulamanın sonuna Console.ReadKey() satırını eklemeyin.
2. Veya Docker'da çalışırken -it parametresini kullanmayın (ancak bu durumda logları canlı izleyemezsiniz).
3. En iyisi, uygulamanızı konsol uygulaması olarak değil, bir servis (Windows Service / Linux Daemon) olarak tasarlamaktır.

- **Q** Bir servisin kodunda güncelleme yaptıktan sonra ne yapmalıyım? Sadece o servisi yeniden build edip çalıştırmalı mıyım? Evetse nasıl? 
- **A** Evet, sadece o servisi yeniden build edip çalıştırmalısın. Bunun için şu komutları kullanabilirsin:
  1. `docker-compose build <service-name>`
  2. `docker-compose up -d <service-name>`

- **Q** Önceki soruya ek: docker-compose.yml docker dizinindeyse komut nasıl olmalı?
- **A** `docker-compose -f docker/docker-compose.yml build <service-name>`
  `docker-compose -f docker/docker-compose.yml up -d <service-name>`

- **Q** simulator (service) ve nexus-simluator (container) arasındaki fark nedir?
- **A** simulator (service): docker-compose.yml dosyasındaki service tanımıdır.
nexus-simluator (container): docker-compose.yml dosyasındaki service tanımına göre oluşturulan container'dır.


- **Q** Monolitik yapıda RabbitMQ kullanılamaz mı? (done)
- **A** Kesinlikle kullanılabilir. Monolitik bir uygulamada, uzun süren işleri (örneğin e-posta gönderimi, ağır rapor oluşturma) arka plana atmak ve sistemin ana işleyişini yavaşlatmamak için RabbitMQ harika bir araçtır. En büyük avantajı, uygulama kapansa veya çökse bile mesajların RabbitMQ üzerinde güvenle beklemeye devam etmesidir (Durability).

- **Q** EventBus/MediatR monolitik yapılarda mı tercih edilir? (done)
- **A** Evet, uygulama içindeki sınıfların birbirine sıkı sıkıya bağlı (Tight Coupling) olmasını engellemek için MediatR gibi "In-Memory Event Bus" çözümleri monolitte standarttır. Bellek hızında çalışırlar ve kurulumları basittir. Ancak "In-Memory" oldukları için uygulama çökerse kuyruktaki tüm olaylar kaybolur. Bu yüzden kritik verilerin taşındığı mikroservis dünyasında RabbitMQ veya Kafka gibi "Distributed Event Bus" çözümleri tercih edilir.

- **Q** Processor içinde, yani monolitik yapıda, RabbitMQ kullanmamak için verdiğin gerekçeleri tatmin edici bulmadım. (done)
- **A** Haklısın, önceki cevabım sığ kaldı. Monolitik yapıda RabbitMQ kullanmanın asıl gerekçesi "Uygulama İçi Kuyruk" (In-Memory Queue) yerine "Dayanıklı Kuyruk" (Persistent Queue) ihtiyacıdır. 
  1. **Hata Toleransı:** Uygulama çökerse bellekteki veri gider, RabbitMQ'daki gitmez. 
  2. **Yük Dengeleme (Throttling):** Uygulamanın kaldıramayacağı kadar yoğun bir işlem (örn: 10.000 mail) gelirse, uygulamayı kilitler. RabbitMQ bu yükü sıraya dizer. 
  Ancak bizim projemiz bir "Mikroservis" projesi olduğu için, RabbitMQ'yu servisler arası "iletişim hattı" (decoupling) olarak kullanmak asıl amacımız. Monolit içinde kullanmak teknik olarak mümkün ama projenin mikroservis karakterine uymadığı için "ayrı servis" yolunu önerdim.

- **Q** Kafka vs RabbitMQ?
- **A** Kafka bir Event Stream'dir; hızlıca akar gider. RabbitMQ ise Message Broker'dır. Alarmlar kritiktir. Bir alarm üretildiğinde, Dashboard kapalı olsa bile o mesajın RabbitMQ'da "beklemesi" ve Dashboard (veya SignalR Hub) açıldığında ona ulaştırılması (guaranteed delivery) gerekir. Bu yüzden alarmlar için RabbitMQ'yu seçiyoruz.

- **Q** ben proto dosyalarının sadece gRPC ile ilişkili olduğunu sanıyordum. öylr değil mi? KC
- **A** Hayır, sadece gRPC ile ilişkili değildir. Protocol Buffers (Protobuf) aslında JSON veya XML gibi bir veri serileştirme formatıdır.

gRPC, bu formatı haberleşme protokolü olarak kullanır ancak Protobuf'ı gRPC olmadan da;

Mesaj kuyruklarında (RabbitMQ, Kafka) veriyi çok küçük boyutlarla saklamak,
Dosya sistemine veri kaydetmek,
Farklı diller (C#, Python, Go) arasında ortak veri modeli (Contract) oluşturmak, için kullanabilirsin.
Özetle: Protobuf bir dil (serileştirme), gRPC ise bu dili kullanan bir telefon (iletişim kanalıdır). Alarmları RabbitMQ üzerinden gönderirken Protobuf kullanmak performansı artırır.

- **Q** telemetry.proto ve alertmessage.proto arasındaki fark nedir? Birinde service tanımlandı, diğerinde tanımlanmadı. Neden? (done)
- **A** 
  - **telemetry.proto (gRPC):** İçinde `service` tanımı olduğu için `Grpc.Tools` arka planda sadece mesaj sınıflarını değil, aynı zamanda **`TelemetryServiceBase` (Sunucu için)** ve **`TelemetryServiceClient` (İstemci için)** sınıflarını da üretir. Bu, iki uygulamanın gRPC protokolüyle doğrudan el sıkışmasını sağlar.
  - **alertmessage.proto (Mesaj Katmanı):** Sadece `message` tanımlandığı için sadece `AlertMessage` isimli bir C# sınıfı (DTO/POCO) üretilir. Biz bu mesajı gRPC üzerinden değil, RabbitMQ üzerinden "ham veri" (byte array) olarak göndereceğimiz için bir "servis/kanal" tanımına ihtiyacımız yok. Sadece verinin yapısının her iki tarafta (gönderen ve alan) aynı olması yeterli.

- **Q** Önceki soruya devam: Mesaj alışverişi için gRPC'de bir class ve method yok, ama RabbitMQ'ta zaten bir class ve method var diye mi ilkinde bir class ve metod tanımlı, ikincisinde değil? (done)
- **A** Evet, tam olarak öyle. 
  - **gRPC:** Doğrudan bir "Uzak Metot Çağrısı" (RPC) olduğu için, "Hangi metot çağrılacak?" sorusunun cevabı olan `service` tanımını protobuff dosyasında belirtmek zorundayız. `Grpc.Tools` bu tanıma bakarak `TelemetryServiceBase` ve `TelemetryServiceClient` sınıflarını üretir.
  - **RabbitMQ:** Bizim için sadece bir "posta kutusu" (Queue) veya "mesajlaşma kanalı"dır. İçine ne koyduğumuzla (AlertMessage) ilgilenmez, sadece "al ve gönder" yapar. Bu yüzden RabbitMQ için bir `service` tanımına ihtiyacımız yoktur. Bizim "mesaj gönderme metodu"muz, `RabbitMQProducer` sınıfının içindeki `PublishAsync` metodudur.

  - **Q** AlertProcessor > appsettings.json > Kafka > GroupId neden var? (done)
- **A** Kafka'da her "Consumer" (Tüketici) bir gruba ait olmalıdır.
  - **Scaling (Ölçekleme):** Eğer aynı `GroupId` ile 3 adet AlertProcessor çalıştırırsan, Kafka gelen verileri bu 3 servis arasında paylaştırır. Böylece sistem daha hızlı çalışır.
  - **Tracking (Takip):** Servis durduğunda, Kafka bu grubun en son hangi mesajda kaldığını hatırlar. Servis tekrar açıldığında kaldığı yerden (offset) devam eder. Veri kaybını önler.

- **Q** AlertProcessor > appsettings.json > RabbitMQ > ExchangeName neden var? (done)
- **A** Bir benzetmeyle açıklayalım:
  - **Direct to Queue (Kuyruğa direkt):** Bir mektubu doğrudan birinin posta kutusuna atmaktır. Eğer mektubun bir kopyasını başkasına da vermek istersen, kodu değiştirip tekrar göndermen gerekir.
  - **Exchange (Postane Ayrıştırma Merkezi):** Sen mektubu postaneye (Exchange) verirsin. Postane, üzerindeki "Alarm" damgasına bakar ve sisteme kayıtlı tüm alıcılara (Kuyruklara) birer kopya dağıtır.
  - **Faydası:** İleride bir "SMS Servisi" eklediğimizde, `AlertProcessor` koduna dokunmayız. Sadece RabbitMQ arayüzünden yeni bir kuyruğu bu Exchange'e bağlarız (Binding). Exchange, gelen alarmı otomatik olarak hem Dashboard'a hem de SMS servisine dağıtır. Kodun "yalnızca bir yere haber verme" kısıtından kurtuluruz.

- **Q** Kafka > Topic ile RabbitMQ > Exchange aynı şey mi? (bu hala eksik. uzun uzun anlat. kısa kesme.)
- **A** Kesinlikle hayır, görevleri ve çalışma mantıkları tamamen farklıdır:
  - **Kafka > Topic (Log-Based Service):** Bir "diske yazılan defter" veya "arşiv" gibidir. Veri buraya geldiğinde hemen silinmez, diske kaydedilir. Tüketiciler (Consumer) o an aktif olmasa bile veri orada bekler. Hatta bir tüketici bağlandığında "bana son 2 saatin verisini baştan ver" (Replay) diyebilir. Kafka'nın ana odağı **Yüksek Hacimli Veri Saklama ve Analiz**dir.
  - **RabbitMQ > Exchange (Message Routing Service):** Bir "akıllı trafik polisi" veya "santral" gibidir. Görevi veriyi saklamak değil, gelen verinin tipine/etiketine bakıp onu ilgili alıcılara (Kuyruklara) en hızlı ve güvenli şekilde yönlendirmektir. Eğer exchange'e bağlı bir kuyruk yoksa, mesaj o an çöpe gider (geçici/transient). RabbitMQ'nun ana odağı **Mesaj Yönlendirme ve Garantili Teslimat**tır.
  - *Özet:* Kafka veriyi bekleten bir "havuz", RabbitMQ veriyi fırlatan bir "sapan"dır.

- **Q** Ingestion ile Processor arasında neden Kafka var? (done)
- **A** Bu microservice dünyasında "Loose Coupling" (Gevşek Bağlılık) ve "Resiliency" (Dayanıklılık) için şarttır:
  1. **Backpressure (Geri Basınç):** IoT cihazlarından saniyede 100 bin veri geldiğini düşün. Processor bu veriyi veritabanına aynı hızda yazamazsa sistem kitlenir. Kafka burada devasa bir "Buffer" (Şok emici) görevi görür. Veriyi yığar, Processor kendi gücü yettiği hızda tüketir.
  2. **Fan-out (Çoğaltma):** Ingestion veriyi bir kez Kafka'ya atar. Bu veriyi hem `Processor` (Redis'e yazmak için) hem `AlertProcessor` (Alarm için) hem de ilerde eklenecek bir `Archiver` (Loglamak için) birbirinden bağımsız olarak okuyabilir. Ingestion'ın bu üçünden de haberi yoktur.
  3. **Hata Toleransı:** Processor servisini güncellerken veya servis çöktüğünde veriler kaybolmaz. Kafka veriyi saklamaya devam eder. Processor geri geldiğinde "Nerede kalmıştım?" diyerek aradaki farkı (lag) hızlıca kapatır.

- **Q** RabbitMQ > QueueBindAsync() metodu ne işe yarar? (done)
- **A** Postanedeki "Ayrıştırma Kutusu" (Exchange) ile "Alıcı Kutusu" (Queue) arasındaki fiziksel bağı kurar. Bu metodu çağırmazsan, Exchange gelen mektubu hangi kutuya atacağını bilemez ve mektup (mesaj) çöpe gider.

- **Q**
  Kafka > AutoOffsetReset.Earliest nedir? (done)
- **A** Bir ses kaydını dinlemeye başladığında; "Eğer daha önce nerede kaldığımı hatırlamıyorsan (yeni bir GroupId ise), kasedi en başa sar ve her şeyi dinle" demektir. Eğer `Latest` dersen, sadece sen bağlandıktan sonra gelen yeni mesajları dinlersin, eskiyi kaçırırsın.

- **Q**
    private IConnection _rabbitConnection;
    private IChannel _rabbitChannel;
  warning CS8618: Non-nullable field '_rabbitConnection' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. (done)
- **A** C# "Nullable Reference Types" özelliği, bir değişkenin null olabileceğini açıkça belirtmeni ister. 
  - `private IConnection _rabbitConnection;` dersen, C# "Bunu constructor'da doldurman lazım, yoksa null kalır" diye uyarır. 
  - Biz `SetupRabbitMQAsync` içinde (yani constructor dışında) doldurduğumuz için, tipi `IConnection?` (nullable) yaparak "Evet, bu başlangıçta null olabilir, ben bunu yönetiyorum" demiş oluyoruz. Hata değil, bir hatırlatmadır.

- **Q** Ingestion ile Processor arasında neden Kafka var? (done)
- **A** Bu modern sistemlerin "Şok Emicisi"dir (Backpressure Handling).
  1. **Hız Farkı:** Ingestion (Giriş) saniyede 10.000 veri alabilirken, Processor (İşlemci) veritabanına yazarken yavaşlayabilir. Kafka araya girerek veriyi biriktirir, Processor nefes aldıkça veriyi çeker.
  2. **Decoupling (Bağımsızlık):** Ingestion sadece Kafka'ya "at ve unut" der. Arkada kaç tane Processor var, Processor bozuk mu, umursamaz.
  3. **Hata Toleransı:** Processor servisini güncellerken veya servis çöktüğünde veriler kaybolmaz. Kafka veriyi saklamaya devam eder. Processor geri geldiğinde "Nerede kalmıştım?" diyerek aradaki farkı (lag) hızlıca kapatır.

- **Q** Kafka'dan veri okurken neden "input ended unexpectedly" hatası aldık? (done)
- **A** Veri tipi uyuşmazlığı (Serialization Mismatch). 
  - Gönderen (Ingestion): Veriyi **JSON** (Metin) tipinde gönderdi.
  - Alan (AlertProcessor): Veriyi **Protobuf** (Binary) tipinde çözmeye çalıştı.
  - Sonuç: `ParseFrom` metodu, JSON karakterlerini anlamlı bir Protobuf yapısına oturtamadığı için veri bozukmuş gibi hata verdi. Dağıtık sistemlerde tüm servislerin aynı serileştirme protokolünü kullandığından emin olunmalıdır.

- **Q** docker-compose -f docker/docker-compose.yml down (done)
- **A** Bu komut;
  1. `up` komutuyla oluşturulan **tüm konteynerleri durdurur ve siler**.
  2. Oluşturulan **ağları (networks) siler**.
  3. Ancak **Volume'leri (verileri) silmez** (bunun için `-v` eklemek gerekir). Yani veritabanındaki veriler kalır ama çalışan makineler ortadan kalkar. Sistemi temizleyip baştan başlatmak için kullanılır.

- **Q** "Kodun içindeki localhost değerlerini ezmek için Docker Compose üzerinden değişkenler tanımlayacağız." Bunu biraz açalım. (done)
- **A** 
  - **Varsayılan:** Kodun içindeki `appsettings.json` dosyasında `BootstrapServers: "localhost:9092"` yazar. Bu, kodun çalıştığı makinede (localhost) arama yapar.
  - **Docker Ortamı:** Docker içinde `localhost` o konteynerin kendisidir. Kafka ise `nexus-kafka` isimli başka bir konteynerdedir.
  - **Ezme (Override):** `docker-compose.yml` içindeki `environment` bölümüne `Kafka__BootstrapServers=kafka:29092` yazdığımızda, .NET Core mimarisi bunu algılar ve `appsettings.json` içindeki değeri yok sayıp bu yeni değeri kullanır. Böylece kodun içine dokunmadan, dışarıdan (environment variable ile) ayarı değiştirmiş oluruz.


- **Q** kafka'da hangisi daha büyük? group? topic? (done)
- **A** **Topic** daha büyüktür (hiyerarşik olarak).
  - **Topic:** Verinin aktığı nehir. (Örn: `telemetry` nehri).
  - **Consumer Group:** O nehir kenarına kurulmuş bir köy. (Örn: `processor-group` köyü).
  - Bir Topic'e birden fazla Group bağlanabilir (Aynı nehir suyunu hem köy A hem köy B kullanabilir). Yani Topic, Grupları kapsayan/besleyen ana yapıdır.

  - **Q** docker-compose.yml'daki kafka:29092 ile KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092,PLAINTEXT_INTERNAL://kafka:29092 ilişkili mi?

- **Q** docker-compose.yml > alertprocessor > restart: always neden yok? crash loop diğer container'lar için de geçerli değil mi?

- **Q** docker-compose -f docker/docker-compose.yml up --build -d
docker ne zaman cache'ten alır, ne zaman yeniden oluşturur? Burada oluşturulduğu söylenen şey container mı, image mı?
--build param kullanılmamalı!!!
docker-compose -f docker/docker-compose.yml up -d


- **Q** Her bir proje dizinine .dockerignore dosyası eklemnin faydaları nedir? (done)
  - **A:**
    1.  **Build Hızı:** Docker, build işleminin başında projedeki dosyaları kendi "context"ine kopyalar. `bin`, `obj`, `.git` gibi binlerce küçük dosyayı kopyalamak dakikalar sürebilir. `.dockerignore` ile bunları hariç tutarak bu süreyi saniyelere indiririz.
    2.  **Imaj Boyutu:** Gereksiz dosyaların son Docker imajına girmesini engelleyerek imajın daha hafif (lightweight) olmasını sağlar.
    3.  **Güvenlik:** Yerel bilgisayardaki gizli ayar dosyalarının (`appsettings.json`'daki API keyler veya backup dosyaları) kazara imaja dahil edilmesini önler.

- **Q** Bu projedeki event-driven bölümler neler? Görüyorum ki sadece RabbitMQ'lu kısım için bu terimi kullanıyorsun. (done)
  - **A:** Aslında projede **iki aşamalı** bir "olay tabanlı" (event-based) yapı var:
    1.  **Telemetry Data (Kafka - Stream-Driven):** Cihazın her gönderdiği veri aslında bir "telemetri olayıdır". Ancak bu veriler çok yoğun ve sürekli olduğu için buna genelde "Streaming" diyoruz. Processor bu akışı dinleyip sistemi güncel tutar.
    2.  **Alerts (RabbitMQ - Event-Driven):** İşte asıl "Event-Driven" ruhu burada. Sıradan telemetriden farklı olarak, "Sıcaklık 50'yi geçti!" durumu **iş kurallarına dayalı kritik bir olay (event)**'dir. Bu event oluştuğunda sistem bir çığlık atar (RabbitMQ Publish) ve bu sesi duyan her servis (Dashboard, SMS, Mail) kendi işini yapar.
    - **Neden RabbitMQ için vurguladık?** Çünkü Kafka'daki telemetri genellikle "veri hamallığı"dır. RabbitMQ'daki alarm ise "anlamlı bir aksiyon tetikleyicisi"dir. Modern mimaride "Event-Driven" dendiğinde genellikle bu tip aksiyon odaklı mesajlaşmalar kastedilir.

- **Q** RabbitMQ, event-driven, gRPC. Bu üçü birbiriyle nasıl ilişkilidir? (done)
  - **A:** Bu üç teknolojiyi bir "Kargo Şirketi" gibi düşünebilirsin:
    1.  **Protobuf (Paketleme):** Verinin en küçük ve hızlı şekilde paketlenmesidir. (Kargoyu kutuya koymak).
    2.  **gRPC (Özel Kurye):** İki servis arasındaki hızlı ve direkt iletişim hattıdır. (Vip-Kurye ile kapıdan kapıya teslimat).
    3.  **RabbitMQ (Dağıtım Merkezi):** Bir servisin attığı paketi (event) alıp, ihtiyacı olan onlarca servise dağıtan merkezdir.
    4.  **Event-Driven (Sistem):** Paketin yola çıkmasıyla (Olay) tüm sistemin harekete geçmesidir.
    - **Özetle:** Servisler **Protobuf** ile paketledikleri verileri, bazen **gRPC** ile doğrudan birbirine, bazen de **RabbitMQ** (Event-Driven) üzerinden tüm sisteme duyurarak çalışırlar.

- **Q** "gRPC ile konuşan servisler, RabbitMQ aracılığıyla birbirlerine "olaylar" (events) gönderererk ..." ifadesindeki "konuşma" ile "event gönderme" aynı şey mi? (done)
  - **A:** Hayır, aynı şey **değildir**, ancak birbiriyle **ilişkilidir**:
    1.  **Konuşma (gRPC):** Bu, servislerin birbirini **tanıması ve komut alıp vermesi**dir. Örneğin, "Dashboard servisi, bana son 10 veriyi getir" dediğinde bu bir gRPC konuşmasıdır. Bu konuşma **senkron** çalışır (Soru sorarsın, cevap beklersin).
    2.  **Event Gönderme (RabbitMQ):** Bu, servislerin birbirine **haber salmasıdır**. Örneğin, "Sıcaklık 50 oldu!" diye bir mesaj yayınlamaktır. Bu mesajı alan servisler kendi işlerini yaparlar. Bu işlem **asenkron** çalışır (Mesajı atarsın, cevap beklemezsin).
    - **İlişki:** Servisler, RabbitMQ'ya mesaj atmak için bile gRPC kullanırlar. Yani **gRPC, RabbitMQ'ya bağlanma ve mesaj gönderme işini yapan teknolojidir**.

- **Q** "Servisler, RabbitMQ'ya mesaj atmak için bile gRPC kullanırlar.""
Bu ifadedeki "mesaj atmak" "event göndermek" midir? (done)
- **A:** Evet, bu bağlamda **aynı anlama gelir**. 
  - **Event Gönderme:** Bir olayın (Alert) diğer servislere duyurulmasıdır.
  - **Mesaj Atmak:** Bu duyurunun RabbitMQ üzerinden yapılmasıdır.
  - Teknik olarak "Event" (Olay) RabbitMQ'ya "Message" (Mesaj) olarak gönderilir. Dolayısıyla "Event Göndermek" ile "Mesaj Atmak" eş anlamlıdır.

- **Q** "SignalR Hub'larını barındırmak (host etmek) için bir Web sunucusuna (Kestrel) ihtiyacımız var." Neden? (done)
  - **A:** SignalR, tarayıcılarla (Browser) gerçek zamanlı iletişim kuran bir **Web teknolojisidir**. Tıpkı bir web sitesinin çalışması gibi, SignalR'ın da çalışabilmesi için bir **HTTP sunucusuna** ihtiyacı vardır. Kestrel, .NET Core'un kendi içinde gelen hafif ve hızlı web sunucusudur. SignalR Hub'ları bu sunucu üzerinden dış dünyaya açılır ve tarayıcılardan gelen WebSocket bağlantılarını yönetir.

- **Q** "Bu servis hem bir RabbitMQ Consumer (kapalı devre dinleyici) olacak, hem de dışarıya WebSocket kapılarını açacak." Bu ne demek? (done)
  - **A:** Bu ifade, servisin **iki yönlü bir görev** üstlendiğini anlatıyor:
      1.  **İçeride (RabbitMQ - Kapalı Devre):** Servis, RabbitMQ kuyruğunu dinleyerek arka planda sürekli gelen "Alert" (Uyarı) mesajlarını yakalar. Bu işlem sadece kendi iç sisteminde gerçekleşir, dışarıdan kimse bu mesaja erişemez. Bu yüzden "kapalı devre dinleyici" denir.
      2.  **Dışarıda (WebSocket - Açık Kapı):** Servis, aynı zamanda bir SignalR Hub'ı barındırır. Bu, servisin dış dünyaya (tarayıcılara, mobil uygulamalara) "kapılarını açtığı" anlamına gelir. Tarayıcılar bu kapıdan bağlanarak anlık olarak "Sıcaklık 50 derece!" gibi uyarıları anında alır.
  - **Özetle:** Servis, içerideki tehlikeyi (Alert) dinler ve bu tehlikeyi dışarıdaki kullanıcılara (WebSocket) anında bildirir.

- **Q** docker-compose.yml dosyasını neden docker dizini altına koyduk?
- **A** Bu tamamen organizasyon ve temizlik tercihi.
    Dizin Temizliği: Proje kök dizini (root) zaten .sln, .gitignore, README gibi dosyalarla kalabalık. Altyapı (Docker, Terraform, Scripts vb.) dosyalarını kendi klasöründe toplamak daha profesyonel bir yaklaşımdır.
    Genişleme: İleride sadece tek bir Compose değil; docker-compose.prod.yml, docker-compose.test.yml gibi dosyalar eklediğinde hepsi derli toplu tek bir yerde durur.

- **Q** Mimarideki zookeeper katmanı nedir ve görevi nedir? (done)
  - **A:** Zookeeper, Kafka'nın **"Koordinatörü"**dür.
    1.  **Health Check:** Hangi Kafka sunucusunun (broker) hayatta olduğunu takip eder.
    2.  **Leader Election:** Bir sunucu çökerse, verileri yönetme görevini kime vereceğini belirler.
    3.  **Metadata Management:** Topic'lerin ve partition'ların bilgilerini saklar.
    **Kısacası:** Zookeeper olmadan Kafka, bir orkestra şefi olmayan orkestra gibi dağılır. (Not: Modern Kafka sürümlerinde bu görev Kafka'nın içine gömülmeye başlandı ama hala yaygın kullanılıyor).

- **Q** ben proto dosyalarının sadece gRPC ile ilişkili olduğunu sanıyordum. öyle değil mi?
- **A** Hayır, sadece gRPC ile ilişkili değildir. Protocol Buffers (Protobuf) aslında JSON veya XML gibi bir veri serileştirme formatıdır.

  gRPC, bu formatı haberleşme protokolü olarak kullanır ancak Protobuf'ı gRPC olmadan da;

  Mesaj kuyruklarında (RabbitMQ, Kafka) veriyi çok küçük boyutlarla saklamak,
  Dosya sistemine veri kaydetmek,
  Farklı diller (C#, Python, Go) arasında ortak veri modeli (Contract) oluşturmak, için kullanabilirsin.
  Özetle: Protobuf bir dil (serileştirme), gRPC ise bu dili kullanan bir telefon (iletişim kanalıdır). Alarmları RabbitMQ üzerinden gönderirken Protobuf kullanmak performansı artırır.

- **Q** CancellationToken nedir ve ne işe yarar? Aşağıdaki örnekte stoppingToken nerede/ne zaman set edilerek akışın durması sağlanabilir? (done)
  - **A:** CancellationToken, bir işlemin "iptal edilebilir" olduğunu belirten bir bayraktır. 
    1. **Neden var?** Uzun süren bir işlem (örn: veri beklemek) sırasında uygulama kapanırsa, o işlemin sonsuza kadar thread'i meşgul etmesini engellemek için kullanılır.
    2. **Kim Set Eder?** ASP.NET Core veya Worker Service altyapısı, uygulama durdurulduğunda (örn: Docker `stop` veya Ctrl+C) bu token'ı otomatik olarak "İptal Edildi" (Cancelled) durumuna getirir.
    3. **Akış Nasıl Durur?** `await ...Async(stoppingToken)` dediğinde, metod bu token'ı kontrol eder. Eğer token iptal edildiyse, metod o satırda durur ve güvenli bir şekilde fonksiyondan çıkar.

- **Q** İkisi arasındaki fark nedir?
_hubConnection!.DisposeAsync();
_hubConnection?.DisposeAsync(); (done)
  - **A:** 
    1. **`?.` (Null-conditional):** "Eğer nesne null değilse metodunu çağır, null ise hiçbir şey yapma." Güvenlidir, hata fırlatmaz.
    2. **`!.` (Null-forgiving):** "Bu nesnenin null olmadığını garanti ediyorum, derleyici sen sus ve çalıştır!" demektir. 
    - **Tehlike:** Eğer nesne o an gerçekten `null` ise (örn: bağlantı hiç kurulmadıysa), `!.` kullanımı uygulamayı **NullReferenceException** ile patlatır. Blazor sayfalarında her zaman `?.` kullanmak best practice'dir.
 (done)
  - **A:** 
    1. **`?.` (Null-conditional):** "Eğer nesne null değilse metodunu çağır, null ise hiçbir şey yapma." Güvenlidir, hata fırlatmaz.
    2. **`!.` (Null-forgiving):** "Bu nesnenin null olmadığını garanti ediyorum, derleyici sen sus ve çalıştır!" demektir. 
    - **Tehlike:** Eğer nesne o an gerçekten `null` ise (örn: bağlantı hiç kurulmadıysa), `!.` kullanımı uygulamayı **NullReferenceException** ile patlatır. Blazor sayfalarında her zaman `?.` kullanmak best practice'dir.

- **Q** Bunlar nedir?
InteractiveServerRenderMode
@rendermode @(new InteractiveServerRenderMode(prerender: false))

- **Q** Console.WriteLine vs logger.LogInformation