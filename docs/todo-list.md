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
