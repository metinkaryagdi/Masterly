# Proje Durum Raporu

Rapor tarihi: 2026-07-09

Bu rapor, mevcut kod tabanindan okunan durumu ozetler. Odak noktasi projenin genel ilerleyisi, sorularin nasil olusturuldugu, gunluk calisma planina nasil girdigi ve soru cesitliliginin hangi mekanizmalarla saglandigidir.

## Genel Durum

Proje, .NET backend becerileri icin adaptif bir egitim platformudur. Backend tarafinda ASP.NET Core 8, Clean Architecture, CQRS benzeri command/query ayrimi, EF Core ve PostgreSQL kullanilmaktadir. Frontend tarafinda `app/` klasoru altinda statik HTML ve React/Babel tabanli prototip ekranlar vardir.

Mevcut ana moduller:

- Kimlik dogrulama ve JWT tabanli oturum.
- Onboarding akisi ve kullanici tercihleri.
- Konu havuzu ve konu bagimliliklari.
- Soru havuzu, quiz cevaplama ve degerlendirme.
- Gunluk calisma plani uretimi.
- Ustalik skoru, tekrar plani ve unutma riski hesaplama.
- Kodlama ve senaryo challenge akislari (Lab).
- Dashboard/analitik gorunumleri.
- Opsiyonel Ollama tabanli AI geri bildirimi.
- AI ile Turkce soru uretimi ve uretilen sorunun deterministik denetimi (audit).

Not: Soru havuzu, aciklamalar, secenekler ve challenge icerikleri Turkce'ye cevrildi. Lab (kod/senaryo) arayuzu tamamen Turkce yerellestirildi.

Kod tabani test yapisi da iceriyor. `tests/TrainingPlatform.UnitTests` servis ve domain davranislarini, `tests/TrainingPlatform.IntegrationTests` API akislari ve endpoint davranislarini kapsiyor.

## Soru Havuzu ve Uretim Mantigi

Temel kaynak, `src/TrainingPlatform.Infrastructure/Seeding/TrainingPlatformSeeder.cs` icindeki elle hazirlanmis, kuralli ve Turkce bir soru havuzudur. Buna ek olarak sorular AI ile de uretilip denetimden gecirilerek havuza eklenebilir (asagidaki "AI ile Soru Uretimi ve Denetim" bolumu).

API baslarken seeder calisir:

- Veritabani migrasyonlari uygulanir veya test ortaminda sema olusturulur.
- Konular yoksa seed edilir.
- Soru havuzu her baslangicta tamamlanir.
- Mevcut sorular, `TopicId + Prompt` kombinasyonu ile kontrol edilir.
- Ayni konu ve ayni prompt varsa tekrar eklenmez.
- Yeni soru eklemek icin mevcut prompt'u degistirmek yerine yeni entry eklemek beklenir.

Sorular runtime'da da eklenebilir. `POST /api/questions` endpoint'i yeni soru olusturur. Bu yolla eklenen sorular konuya, soru tipine, zorluga, cozum suresine, gecme skoruna, etiketlere, kabul edilen cevaplara ve seceneklere sahip olur.

## AI ile Soru Uretimi ve Denetim (Audit)

Artik sorular AI ile uretilip otomatik denetimden gecirilebiliyor. `POST /api/questions/generate` endpoint'i su akisi calistirir:

- Konu, soru tipi, zorluk ve opsiyonel etiketlerle bir uretim istegi olusturulur.
- Ollama modeli, katı JSON dondurecek sekilde ve tamamen Turkce icerik uretmesi icin yonlendirilir (`PromptCatalog`). Model yaniti markdown/```json cercevelerinden temizlenip ilk JSON nesnesi ayristirilir.
- Uretilen aday, `GeneratedQuestionAuditor` tarafindan deterministik olarak denetlenir. Bu denetim tamamen saf (yan etkisiz) oldugu icin model calismadan da birim testleriyle dogrulanabilir.
- Denetim gecerse soru havuza kaydedilir; gecmezse kaydedilmez ve reddedilme gerekceleri Turkce olarak dondurulur (API 200 vs. 422).
- Model denetimden gecen bir soru uretene kadar `MaxAttempts` (varsayilan 3) kez tekrar denenir.

Denetimin (audit) kontrol ettikleri:

- Soru metni bos degil ve makul uzunlukta (12-4000 karakter); aciklama bos degil.
- Yinelenen soru kontrolu: ayni konuda normalize edilmis ayni prompt varsa reddedilir.
- Dil kontrolu: metin belirgin sekilde Ingilizce ise (Ingilizce islev sozcuklerinden en az iki tanesi) reddedilir; hedef Turkce.
- `MultipleChoice`: 2-6 secenek, tam olarak bir dogru secenek, bos olmayan ve benzersiz secenek metinleri.
- `ShortAnswer`/`Scenario`: en az bir kabul edilen cevap/anahtar kelime ve secenek icermemesi.

## Soru Modeli

Bir soru asagidaki alanlarla tutulur:

- `TopicId`: Sorunun ait oldugu konu.
- `QuestionType`: Soru tipi.
- `Prompt`: Kullaniciya gosterilen soru metni.
- `Explanation`: Cevap sonrasi aciklama.
- `Difficulty`: Zorluk seviyesi.
- `EstimatedSolvingTimeSeconds`: Tahmini cozum suresi.
- `MinimumPassingScore`: Gecmek icin gereken minimum skor.
- `Tags`: Konu/alt beceri etiketleri.
- `AcceptedAnswers`: Kisa cevap ve senaryo sorulari icin kabul edilen cevaplar veya anahtar kelimeler.
- `Options`: Coktan secmeli sorular icin cevap secenekleri.

Desteklenen soru tipleri:

- `MultipleChoice`: Coktan secmeli, tam olarak bir dogru secenek bekler.
- `ShortAnswer`: Kisa metin cevabi, kabul edilen cevaplarla eslesir.
- `Scenario`: Acik uclu senaryo cevabi, beklenen anahtar kelime kapsamina gore puanlanir.

## Mevcut Soru Cesitliligi

Seed havuzunda toplam 64 soru vardir. Dagilim dengeli olarak 8 konuya bolunmustur: her konuda 8 soru bulunur.

Konu dagilimi:

| Konu | Soru Sayisi |
| --- | ---: |
| C# Foundations | 8 |
| ASP.NET Core API Design | 8 |
| EF Core | 8 |
| Clean Architecture | 8 |
| CQRS | 8 |
| JWT Authentication | 8 |
| Caching Strategy | 8 |
| PostgreSQL | 8 |

Soru tipi dagilimi:

| Tip | Sayi |
| --- | ---: |
| MultipleChoice | 29 |
| ShortAnswer | 27 |
| Scenario | 8 |

Zorluk dagilimi:

| Zorluk | Sayi |
| --- | ---: |
| Fundamental | 22 |
| Intermediate | 25 |
| Advanced | 17 |

Bu dagilim, her konu icin hem bilgi kontrolu hem kisa cevap hem de en az bir acik uclu senaryo bulunmasini saglar. Ancak senaryo sorulari sayisal olarak daha azdir; her konu basina genellikle 1 senaryo bulunur.

## Gunluk Planda Sorularin Secilmesi

Gunluk plan, tum sorulari rastgele dagitmak yerine kullanicinin ilerlemesine gore secim yapar. Akis `DailyStudyPlanService` uzerinden ilerler.

Secim mantigi:

- Kullanici icin gunluk soru hedefi okunur.
- Konular kullanicinin durumuna gore kategorilere ayrilir.
- Zayif konular hedefin yaklasik %40'ini alir.
- Son calisilan/guncel konular yaklasik %30 alir.
- Guclu konular yaklasik %20 alir.
- Yeni konular yaklasik %10 alir.
- Kategoride yeterli soru veya konu yoksa eksik kalan kota tum uygun konulardan tamamlanmaya calisilir.

Konu kategorileri:

- `Weak`: Ustalik skoru 60 altindaysa veya unutma riski yuksekse.
- `Recent`: Son 7 gun icinde aktivite varsa veya orta seviye durumdaysa.
- `Strong`: Ustalik skoru 80 ve uzeri, unutma riski dusukse.
- `New`: Kullanici icin ilerleme kaydi olmayan konular.

Soru seciminde ayni konunun plani domine etmemesi icin round-robin yaklasimi kullanilir. Her konu icinden once taze sorular secilir. Son 7 gun icinde dogru cevaplanan sorular tekrar one alinmaz; ancak havuz kurursa tekrar sorular da kullanilabilir.

Plan uretimi kullanici ve gun bazinda deterministiktir. Ayni kullanici icin ayni tarih tekrar uretildiginde ayni plan doner; farkli gunlerde havuz rotasyonu degisir.

## Zorluk Uyarlamasi

Sorunun hedef zorlugu kullanicinin konu ustalik skoruna gore belirlenir:

- Ustalik 40 altinda: `Fundamental`
- Ustalik 40-69 arasinda: `Intermediate`
- Ustalik 70 ve uzeri: `Advanced`

Secim sirasinda sorular, hedef zorluga yakinliklarina gore siralanir. Ayni yakinliktaki sorular arasinda gunluk seed'e bagli rastgelelik kullanilir. Bu sayede sistem hem kullanicinin seviyesine yaklasir hem de her gun ayni listeyi tekrar etmeye calismaz.

## Cevap Degerlendirme

Cevaplar deterministik olarak degerlendirilir.

`MultipleChoice`:

- Secilen option id, tek dogru secenekle karsilastirilir.
- Dogruysa 100, yanlissa 0 puan verilir.

`ShortAnswer`:

- Cevap normalize edilir: trim, lowercase ve bosluk sadelestirme.
- Kabul edilen cevaplarla tam eslesme varsa 100 puan.
- Kismi eslesme varsa 70 puan.
- Eslesme yoksa 0 puan.

`Scenario`:

- Kabul edilen cevaplar anahtar kelime gibi kullanilir.
- Kullanici cevabinda gecen anahtar kelime oranina gore skor hesaplanir.
- Skor `MinimumPassingScore` ustundeyse basarili sayilir.

Cevap suresi de hesaplamaya girer. Tahmini cozum suresine gore `speedScore` uretilir. Bu skor dogrudan dogru/yanlis sonucunu degistirmez, ancak tekrar motorundaki kalite ve ustalik skoru hesaplamalarina etki eder.

## Ustalik ve Tekrar Plani

Bir cevap gonderildiginde sistem:

- Soruyu ve konuyu bulur.
- Kullanici icin konu ilerlemesini ve tekrar planini olusturur veya gunceller.
- Cevabi degerlendirir.
- Ustalik skoru, tutarlilik skoru, tekrar araligi, sonraki tekrar tarihi, unutma riski ve oncelik skorunu hesaplar.
- Yanlis cevaplar icin mistake log kaydi olusturur.

Dogru cevaplar ustaligi ve tekrar araligini artirabilir. Yanlis cevaplar tekrar araligini kisaltir, unutma riskini ve onceligi yukseltir.

## Challenge Cesitliligi

Gunluk plan sadece teorik sorulardan olusmaz. Uygun durumda plana bir kodlama challenge'i ve bir senaryo challenge'i da eklenir.

Kodlama challenge'lari iki sekilde calisabilir:

- `TestCode` varsa submission izole runner container icinde xUnit testleriyle calistirilir.
- `TestCode` yoksa review/AI-feedback hattinda kalir.

Senaryo challenge'lari kriter kapsamina gore degerlendirilir. Opsiyonel AI geri bildirimi Ollama ayari aciksa submission'a eklenebilir.

## Guclu Yonler

- Soru havuzu konu bazinda dengeli: 8 konu x 8 soru.
- Uc farkli soru tipi var: coktan secmeli, kisa cevap, senaryo.
- Zorluk seviyeleri kullanici ustaligina gore seciliyor.
- Son 7 gunde dogru cevaplanan sorular ertelenerek tekrar azaltiliyor.
- Gunluk plan zayif konulara agirlik veriyor.
- Cevap sonrasi ustalik, unutma riski ve tekrar tarihi guncelleniyor.
- Sorular API ile runtime'da genisletilebiliyor.

## Sinirlar ve Gelistirme Alanlari

- AI ile soru uretimi ve otomatik denetim eklendi; ancak denetim deterministik/yapisaldir, semantik dogruluk (cevabin gercekten dogru olup olmadigi) hala model kalitesine baglidir.
- AI uretimi Ollama'nin acik ve erisilebilir olmasini gerektirir; kapaliyken yalnizca seed havuzu kullanilir.
- Her konuda seed olarak ~8 soru oldugu icin, AI uretimi olmadan aktif kullanimda havuz hizli tukenebilir.
- Senaryo sorulari toplam 8 adet; acik uclu pratik cesitliligi artirilabilir.
- Kisa cevap degerlendirmesi semantik anlama yapmiyor; normalize edilmis exact/partial match kullaniyor.
- Senaryo degerlendirmesi anahtar kelime kapsamina dayali; kaliteli ama farkli ifadeler eksik puan alabilir.
- `POST /api/questions` ile soru ekleme var, ancak raporda gorulen kadariyla otomatik kalite kontrol, duplicate semantik kontrolu veya admin onay akisi yok.
- Soru havuzunda zorluk dagilimi dengeliye yakin olsa da `Advanced` soru sayisi daha dusuk.

## Sonuc

Projenin mevcut durumunda soru sistemi calisir ve adaptif planlama icin yeterli temel mekanizmalara sahiptir. Sorularin uretim kaynagi elle hazirlanmis seed havuzudur; sistem bu havuzu kullanicinin ustalik skoru, unutma riski, son cevaplari ve gunluk hedefiyle birlestirerek plan uretir.

Cesitlilik su anda konu, soru tipi ve zorluk eksenlerinde saglanmistir. En buyuk gelistirme alani soru havuzunun genisletilmesi, senaryo/kodlama pratiklerinin artirilmasi ve kisa cevap/senaryo degerlendirmesine daha semantik bir kontrol katmani eklenmesidir.
