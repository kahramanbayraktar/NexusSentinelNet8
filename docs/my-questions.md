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